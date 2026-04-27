from flask import render_template, request, session, url_for, redirect
from CTFd.admin import admin
from CTFd.models import Contests, Semester, db
from CTFd.utils.decorators import admins_only

@admin.route("/admin/global", methods=["GET"])
@admins_only
def global_home():
    # Clear active contest from session when entering global admin area
    if 'admin_contest_id' in session:
        session.pop('admin_contest_id')
    return redirect(url_for('admin.semesters_listing'))

@admin.route("/admin/semesters", methods=["GET"])
@admins_only
def semesters_listing():
    # Clear active contest just in case
    if 'admin_contest_id' in session:
        session.pop('admin_contest_id')
        
    semesters = Semester.query.order_by(Semester.id.desc()).all()
    selected_semester_id = request.args.get('semester_id', type=int)
    
    if not selected_semester_id and semesters:
        selected_semester_id = semesters[0].id
        
    contests = []
    if selected_semester_id:
        contests = Contests.query.filter_by(semester_id=selected_semester_id).order_by(Contests.id.desc()).all()
        
    return render_template(
        "admin/semesters/semesters.html",
        semesters=semesters,
        selected_semester_id=selected_semester_id,
        contests=contests
    )

@admin.route("/admin/select_contest/<int:contest_id>", methods=["GET"])
@admins_only
def select_contest(contest_id):
    contest = Contests.query.filter_by(id=contest_id).first_or_404()
    session['admin_contest_id'] = contest.id
    return redirect(url_for('admin.statistics'))

@admin.route("/admin/import_contest_participants", methods=["POST"])
@admins_only
def import_contest_participants():
    from io import StringIO
    import csv
    from CTFd.models import Users, ContestParticipants
    from CTFd.utils.crypto import hash_password
    
    contest_id = request.form.get("contest_id", type=int)
    if not contest_id:
        flash("Invalid contest ID.", "danger")
        return redirect(url_for("admin.semesters_listing"))

    if "csv_file" not in request.files:
        flash("No file provided.", "danger")
        return redirect(url_for("admin.semesters_listing"))

    file = request.files["csv_file"]
    if file.filename == "":
        flash("No file selected.", "danger")
        return redirect(url_for("admin.semesters_listing"))

    try:
        raw = file.stream.read()
        try:
            csvdata = raw.decode("utf-8-sig")
        except UnicodeDecodeError:
            csvdata = raw.decode("latin-1")
            
        csvfile = StringIO(csvdata)
        reader = csv.DictReader(csvfile)
        
        # Normalize column names to lowercase
        def normalize_row(row):
            return {k.strip().lower() if isinstance(k, str) else k: v for k, v in row.items()}
            
        added_users = 0
        added_participants = 0
        
        for row in reader:
            row = normalize_row(row)
            email = row.get("email")
            if not email:
                continue
                
            email = email.strip().lower()
            
            # Check if user exists
            user = Users.query.filter_by(email=email).first()
            if not user:
                # Create a placeholder user
                name = row.get("name", email.split("@")[0])
                password = row.get("password", "fpt123456") # Default password if not provided
                
                user = Users(
                    name=name,
                    email=email,
                    password=hash_password(password),
                    type="user",
                    hidden=False,
                )
                db.session.add(user)
                db.session.commit() # Need to commit to get the user.id
                added_users += 1
                
            # Check if participant already in contest
            participant = ContestParticipants.query.filter_by(
                contest_id=contest_id, user_id=user.id
            ).first()
            
            if not participant:
                participant = ContestParticipants(
                    contest_id=contest_id,
                    user_id=user.id,
                )
                db.session.add(participant)
                added_participants += 1
                
        db.session.commit()
        flash(f"Imported successfully. Created {added_users} new users, added {added_participants} participants to contest.", "success")
        
    except Exception as e:
        db.session.rollback()
        flash(f"Error importing participants: {str(e)}", "danger")
        
    return redirect(url_for("admin.semesters_listing"))
