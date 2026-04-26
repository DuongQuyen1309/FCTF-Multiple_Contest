from flask import render_template

from CTFd.admin import admin
from CTFd.models import Challenges, ContestParticipants, ContestsChallenges, Contests, Fails, Semester, Solves, Teams, Tracking, Users, db
from CTFd.utils.decorators import admins_only, admin_or_challenge_writer_only_or_jury
from CTFd.utils.modes import get_model
from CTFd.utils.updates import update_check


@admin.route("/admin/statistics", methods=["GET"])
@admin_or_challenge_writer_only_or_jury
def statistics():
    Model = get_model()

    user_count = db.session.query(db.func.count(Users.id)).scalar()
    challenge_count = db.session.query(db.func.count(Challenges.id)).scalar()
    total_points = 0
    ip_count = db.session.query(db.func.count(db.func.distinct(Tracking.ip))).scalar()
    wrong_count = (
        db.session.query(db.func.count(Fails.id))
        .join(Model, Fails.account_id == Model.id)
        .filter(Model.banned == False, Model.hidden == False)
        .scalar()
    )
    solve_count = (
        db.session.query(db.func.count(Solves.id))
        .join(Model, Solves.account_id == Model.id)
        .filter(Model.banned == False, Model.hidden == False)
        .scalar()
    )
    semester_count = db.session.query(db.func.count(Semester.id)).scalar()

    solves_sub = (
        db.session.query(
            Solves.contest_challenge_id, db.func.count(Solves.contest_challenge_id).label("solves_cnt")
        )
        .join(Model, Solves.account_id == Model.id)
        .filter(Model.banned == False, Model.hidden == False)
        .group_by(Solves.contest_challenge_id)
        .subquery()
    )

    solves = (
        db.session.query(
            ContestsChallenges.bank_id,
            solves_sub.columns.solves_cnt,
            Challenges.name,
        )
        .join(ContestsChallenges, solves_sub.columns.contest_challenge_id == ContestsChallenges.id)
        .join(Challenges, ContestsChallenges.bank_id == Challenges.id)
        .all()
    )
    # solves is a list of tuples: (bank_id, solves_cnt, name)
    solve_data = {name: count for _bid, count, name in solves}
    most_solved = max(solve_data, key=solve_data.get) if solve_data else None
    least_solved = min(solve_data, key=solve_data.get) if solve_data else None

    recent_contests = (
        db.session.query(Contests)
        .order_by(Contests.created_at.desc())
        .limit(5)
        .all()
    )

    db.session.close()

    return render_template(
        "admin/statistics.html",
        user_count=user_count,
        ip_count=ip_count,
        wrong_count=wrong_count,
        solve_count=solve_count,
        challenge_count=challenge_count,
        total_points=total_points,
        semester_count=semester_count,
        recent_contests=recent_contests,
    )