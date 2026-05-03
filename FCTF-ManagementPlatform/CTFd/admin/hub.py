"""
Admin Hub — trang quản lý chung của admin trong hệ thống multiple contest.

Routes:
  GET  /admin/hub               → redirect sang /admin/hub/semesters
  GET  /admin/hub/semesters     → danh sách semester
  GET  /admin/hub/semesters/new → form tạo semester
  GET  /admin/hub/semesters/<id>/contests → danh sách contest của semester
  GET  /admin/hub/contests/new  → form tạo contest (query ?semester_id=<id>)
  GET  /admin/hub/challenges    → danh sách challenge bank
  GET  /admin/hub/challenges/new → upload/tạo challenge mới
  GET  /admin/hub/users         → danh sách toàn bộ users hệ thống
"""

from flask import redirect, render_template, request, url_for
from sqlalchemy import or_

from CTFd.admin import admin
from CTFd.models import Challenges, Contests, Semester, Tags, Tracking, Users, db
from CTFd.utils.decorators import admins_only
from CTFd.utils.user import is_admin


# ---------------------------------------------------------------------------
# Hub root → redirect to semesters
# ---------------------------------------------------------------------------

@admin.route("/admin/hub")
@admins_only
def hub_index():
    return redirect(url_for("admin.hub_semesters"))


# ---------------------------------------------------------------------------
# Semesters
# ---------------------------------------------------------------------------

@admin.route("/admin/hub/semesters")
@admins_only
def hub_semesters():
    semesters = Semester.query.order_by(Semester.id.desc()).all()
    # Annotate contest count
    for s in semesters:
        s.contest_count = s.contests.count()
    return render_template("admin/hub/semesters.html", semesters=semesters)


@admin.route("/admin/hub/semesters/new")
@admins_only
def hub_semester_new():
    return render_template("admin/hub/semester_new.html")


# ---------------------------------------------------------------------------
# Contests of a semester
# ---------------------------------------------------------------------------

@admin.route("/admin/hub/semesters/<int:semester_id>/contests")
@admins_only
def hub_contests(semester_id):
    semester = Semester.query.get_or_404(semester_id)
    contests = (
        Contests.query
        .filter_by(semester_name=semester.semester_name)
        .order_by(Contests.id.desc())
        .all()
    )
    # Annotate counts
    for c in contests:
        c.participant_count = c.participants.count()
        c.challenge_count = c.challenges.count()
        owner = Users.query.get(c.owner_id) if c.owner_id else None
        c.owner_name = owner.name if owner else "—"
    return render_template(
        "admin/hub/contests.html",
        semester=semester,
        contests=contests,
    )


@admin.route("/admin/hub/contests/new")
@admins_only
def hub_contest_new():
    semester_id = request.args.get("semester_id", type=int)
    semester = Semester.query.get_or_404(semester_id) if semester_id else None
    semesters = Semester.query.order_by(Semester.id.desc()).all()
    return render_template(
        "admin/hub/contest_new.html",
        semester=semester,
        semesters=semesters,
    )


# ---------------------------------------------------------------------------
# Challenge bank
# ---------------------------------------------------------------------------

@admin.route("/admin/hub/challenges")
@admins_only
def hub_challenges():
    q = request.args.get("q", "")
    field = request.args.get("field", "name")
    category = request.args.get("category", "")
    page = abs(request.args.get("page", 1, type=int))

    filters = []
    if q and Challenges.__mapper__.has_property(field):
        filters.append(getattr(Challenges, field).like(f"%{q}%"))
    if category:
        filters.append(Challenges.category == category)

    challenges = (
        Challenges.query
        .filter(*filters)
        .order_by(Challenges.id.desc())
        .paginate(page=page, per_page=50, error_out=False)
    )

    raw_categories = Challenges.query.with_entities(Challenges.category).distinct().all()
    categories = [c[0] for c in raw_categories if c and c[0]]

    for ch in challenges.items:
        author = Users.query.get(ch.author_id) if ch.author_id else None
        ch.author_name = author.name if author else "—"

    args = dict(request.args)
    args.pop("page", None)

    return render_template(
        "admin/hub/challenges.html",
        challenges=challenges,
        prev_page=url_for("admin.hub_challenges", page=challenges.prev_num, **args),
        next_page=url_for("admin.hub_challenges", page=challenges.next_num, **args),
        q=q,
        field=field,
        category=category,
        categories=categories,
    )


@admin.route("/admin/hub/challenges/new")
@admins_only
def hub_challenge_new():
    from CTFd.plugins.challenges import CHALLENGE_CLASSES
    types = list(CHALLENGE_CLASSES.keys())
    return render_template("admin/hub/challenge_upload.html", types=types)


# ---------------------------------------------------------------------------
# Users
# ---------------------------------------------------------------------------

@admin.route("/admin/hub/users")
@admins_only
def hub_users():
    q = request.args.get("q", "")
    field = request.args.get("field", "name")
    role_filter = request.args.get("role", "")
    verified_filter = request.args.get("verified", "")
    page = abs(request.args.get("page", 1, type=int))

    filters = []

    if q and Users.__mapper__.has_property(field):
        filters.append(getattr(Users, field).like(f"%{q}%"))

    if role_filter:
        filters.append(Users.type == role_filter)

    if verified_filter == "true":
        filters.append(Users.verified == True)
    elif verified_filter == "false":
        filters.append(Users.verified == False)

    users = (
        Users.query
        .filter(*filters)
        .order_by(Users.id.asc())
        .paginate(page=page, per_page=50, error_out=False)
    )

    args = dict(request.args)
    args.pop("page", None)

    return render_template(
        "admin/hub/users.html",
        users=users,
        prev_page=url_for("admin.hub_users", page=users.prev_num, **args),
        next_page=url_for("admin.hub_users", page=users.next_num, **args),
        q=q,
        field=field,
        role_filter=role_filter,
        verified_filter=verified_filter,
    )
