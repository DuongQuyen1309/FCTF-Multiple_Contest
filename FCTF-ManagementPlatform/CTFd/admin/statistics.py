from flask import render_template

from CTFd.admin import admin
from CTFd.models import Challenges, ContestParticipants, ContestsChallenges, Contests, Fails, Solves, Teams, Tracking, Users, db
from CTFd.utils.decorators import admins_only, admin_or_challenge_writer_only_or_jury
from CTFd.utils.modes import get_model
from CTFd.utils.updates import update_check


@admin.route("/admin/statistics", methods=["GET"])
@admin_or_challenge_writer_only_or_jury
def statistics():
    Model = get_model()

    from flask import session
    from CTFd.models import ContestsChallenges, ContestParticipants

    admin_contest_id = session.get('admin_contest_id')

    if admin_contest_id:
        teams_q = db.session.query(db.func.count(db.func.distinct(ContestParticipants.team_id))).filter(ContestParticipants.contest_id == admin_contest_id, ContestParticipants.team_id.isnot(None)).scalar_subquery()
        users_q = db.session.query(db.func.count(ContestParticipants.id)).filter_by(contest_id=admin_contest_id).scalar_subquery()
        chals_q = db.session.query(db.func.count(ContestsChallenges.id)).filter_by(contest_id=admin_contest_id).scalar_subquery()
        points_q = (
            db.session.query(db.func.sum(db.func.coalesce(ContestsChallenges.value, Challenges.value)))
            .join(Challenges, ContestsChallenges.bank_id == Challenges.id)
            .filter(ContestsChallenges.contest_id == admin_contest_id)
            .filter(db.func.coalesce(ContestsChallenges.state, Challenges.state) == "visible")
            .scalar_subquery()
        )
        # Tracking currently doesn't easily map to contest_id without joins, simplify for now
        ips_q = db.session.query(db.func.count(db.func.distinct(Tracking.ip))).scalar_subquery()
        
        wrong_q = (
            db.session.query(db.func.count(Fails.id))
            .join(Model, Fails.account_id == Model.id)
            .filter(Model.banned == False, Model.hidden == False)
            .filter(Fails.contest_id == admin_contest_id)
            .scalar_subquery()
        )

        solve_q = (
            db.session.query(db.func.count(Solves.id))
            .join(Model, Solves.account_id == Model.id)
            .filter(Model.banned == False, Model.hidden == False)
            .filter(Solves.contest_id == admin_contest_id)
            .scalar_subquery()
        )
    else:
        teams_q = db.session.query(db.func.count(Teams.id)).scalar_subquery()
        users_q = db.session.query(db.func.count(Users.id)).scalar_subquery()
        chals_q = db.session.query(db.func.count(Challenges.id)).scalar_subquery()
        points_q = (
            db.session.query(db.func.sum(ContestsChallenges.value))
            .scalar_subquery()
        )
        ips_q = db.session.query(db.func.count(db.func.distinct(Tracking.ip))).scalar_subquery()

        wrong_q = (
            db.session.query(db.func.count(Fails.id))
            .join(Model, Fails.account_id == Model.id)
            .filter(Model.banned == False, Model.hidden == False)
            .scalar_subquery()
        )

        solve_q = (
            db.session.query(db.func.count(Solves.id))
            .join(Model, Solves.account_id == Model.id)
            .filter(Model.banned == False, Model.hidden == False)
            .scalar_subquery()
        )

    # executing batch query
    stats = db.session.query(
        teams_q, users_q, chals_q, points_q, ips_q, wrong_q, solve_q
    ).first()
    (team_count, user_count, challenge_count, total_points, ip_count, wrong_count, solve_count) = stats

    solves_query = (
        db.session.query(
            Solves.contest_challenge_id, db.func.count(Solves.contest_challenge_id).label("solves_cnt")
        )
        .join(ContestsChallenges, Solves.contest_challenge_id == ContestsChallenges.id)
        .join(Model, Solves.account_id == Model.id)
        .filter(Model.banned == False, Model.hidden == False)
    )
    
    if admin_contest_id:
        solves_query = solves_query.filter(Solves.contest_id == admin_contest_id)
        
    solves_sub = solves_query.group_by(Solves.contest_challenge_id).subquery()
    
    solves = (
        db.session.query(
            ContestsChallenges.bank_id,
            solves_sub.columns.solves_cnt,
            db.func.coalesce(ContestsChallenges.name, Challenges.name),
        )
        .join(ContestsChallenges, solves_sub.columns.contest_challenge_id == ContestsChallenges.id)
        .join(Challenges, ContestsChallenges.bank_id == Challenges.id)
        .all()
    )
    solve_data = {name: count for _cid, count, name in solves}
    most_solved = max(solve_data, key=solve_data.get) if solve_data else None
    least_solved = min(solve_data, key=solve_data.get) if solve_data else None

    # Semester và recent contests
    from CTFd.models import Semester
    semester_count = Semester.query.count()
    recent_contests = Contests.query.order_by(Contests.created_at.desc()).limit(5).all()

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