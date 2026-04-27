"""
admin/contests.py
CRUD contests + API xem contests theo semester.
"""
import datetime
import re

from flask import flash, jsonify, redirect, render_template, request, url_for

from CTFd.admin import admin
from CTFd.models import db
from CTFd.models import Contests as Contest, Semester, ContestParticipants as ContestParticipant
from CTFd.utils.decorators import admins_only


# ── helpers ───────────────────────────────────────────────────────────────────

def _slugify(text: str) -> str:
    text = text.lower().strip()
    text = re.sub(r"[^\w\s-]", "", text)
    text = re.sub(r"[\s_-]+", "-", text)
    return text[:100]


def _unique_slug(base: str, exclude_id: int | None = None) -> str:
    slug = _slugify(base)
    q = Contest.query.filter_by(slug=slug)
    if exclude_id:
        q = q.filter(Contest.id != exclude_id)
    if q.first():
        slug = f"{slug}-{int(datetime.datetime.utcnow().timestamp())}"
    return slug


def _parse_dt(field: str) -> datetime.datetime | None:
    val = request.form.get(field, "").strip()
    if not val:
        return None
    try:
        return datetime.datetime.fromisoformat(val)
    except ValueError:
        return None


def _contest_json(c: Contest) -> dict:
    return {
        "id": c.id,
        "name": c.name,
        "slug": c.slug,
        "description": c.description,
        "state": c.state,
        "user_mode": c.user_mode,
        "semester_name": c.semester_name,
        "start_time": c.start_time.isoformat() if c.start_time else None,
        "end_time": c.end_time.isoformat() if c.end_time else None,
        "freeze_scoreboard_at": c.freeze_scoreboard_at.isoformat() if c.freeze_scoreboard_at else None,
        "created_at": c.created_at.isoformat() if c.created_at else None,
        "participant_count": ContestParticipant.query.filter_by(contest_id=c.id).count(),
    }


# ── Listing (HTML) ────────────────────────────────────────────────────────────

@admin.route("/admin/contests")
@admins_only
def contests_listing():
    q = request.args.get("q", "").strip()
    state = request.args.get("state", "").strip()
    semester_id = request.args.get("semester_id", "").strip()
    page = abs(request.args.get("page", 1, type=int))

    semesters = Semester.query.order_by(Semester.semester_name).all()

    query = Contest.query
    if q:
        query = query.filter(
            db.or_(Contest.name.ilike(f"%{q}%"), Contest.slug.ilike(f"%{q}%"))
        )
    if state:
        query = query.filter(Contest.state == state)
    if semester_id:
        sem = Semester.query.get(semester_id)
        if sem:
            query = query.filter(Contest.semester_name == sem.semester_name)

    contests = query.order_by(Contest.created_at.desc()).paginate(
        page=page, per_page=20, error_out=False
    )

    args = dict(request.args)
    args.pop("page", None)
    prev_page = url_for("admin.contests_listing", page=contests.prev_num, **args) if contests.has_prev else "#"
    next_page = url_for("admin.contests_listing", page=contests.next_num, **args) if contests.has_next else "#"

    return render_template(
        "admin/contests/listing.html",
        contests=contests,
        semesters=semesters,
        q=q,
        state=state,
        semester_id=semester_id,
        prev_page=prev_page,
        next_page=next_page,
    )


@admin.route("/admin/contests/new", methods=["GET", "POST"])
@admins_only
def contest_new_standalone():
    semesters = Semester.query.order_by(Semester.semester_name).all()

    if request.method == "POST":
        name = request.form.get("name", "").strip()
        if not name:
            flash("Tên contest không được trống.", "danger")
            return render_template("admin/contests/new.html", semesters=semesters)

        slug = _unique_slug(request.form.get("slug", "") or name)

        semester_name = None
        sem_id = request.form.get("semester_id", "").strip()
        if sem_id:
            sem = Semester.query.get(sem_id)
            if sem:
                semester_name = sem.semester_name

        from CTFd.utils.user import get_current_user
        owner = get_current_user()

        contest = Contest(
            name=name,
            description=request.form.get("description", "").strip() or None,
            slug=slug,
            semester_name=semester_name,
            owner_id=owner.id if owner else None,
            state=request.form.get("state", "hidden"),
            user_mode=request.form.get("user_mode", "users"),
            start_time=_parse_dt("start_time"),
            end_time=_parse_dt("end_time"),
            freeze_scoreboard_at=_parse_dt("freeze_scoreboard_at"),
        )
        db.session.add(contest)
        db.session.commit()
        flash(f"Đã tạo contest '{contest.name}'.", "success")
        return redirect(url_for("admin.contest_dashboard", contest_id=contest.id))

    return render_template("admin/contests/new.html", semesters=semesters)


# ── Listing (JSON API) ─────────────────────────────────────────────────────────

@admin.route("/admin/api/contests")
@admins_only
def api_contests_listing():
    """
    GET /admin/api/contests
    Query params:
      - q          : tìm theo name/slug
      - state      : hidden | upcoming | active | paused | ended
      - semester_id: lọc theo semester
      - page       : trang (default 1)
      - per_page   : số item/trang (default 20, max 100)
    """
    q = request.args.get("q", "").strip()
    state = request.args.get("state", "").strip()
    semester_id = request.args.get("semester_id", "").strip()
    page = abs(request.args.get("page", 1, type=int))
    per_page = min(abs(request.args.get("per_page", 20, type=int)), 100)

    query = Contest.query

    if q:
        query = query.filter(
            db.or_(
                Contest.name.ilike(f"%{q}%"),
                Contest.slug.ilike(f"%{q}%"),
            )
        )
    if state:
        query = query.filter(Contest.state == state)
    if semester_id:
        sem = Semester.query.get(semester_id)
        if sem:
            query = query.filter(Contest.semester_name == sem.semester_name)

    pagination = query.order_by(Contest.created_at.desc()).paginate(
        page=page, per_page=per_page, error_out=False
    )

    return jsonify({
        "success": True,
        "contests": [_contest_json(c) for c in pagination.items],
        "pagination": {
            "page": pagination.page,
            "per_page": per_page,
            "total": pagination.total,
            "pages": pagination.pages,
            "has_next": pagination.has_next,
            "has_prev": pagination.has_prev,
        },
    })


# ── Xem contests theo semester (JSON) ─────────────────────────────────────────

@admin.route("/admin/api/semesters/<int:semester_id>/contests")
@admins_only
def api_contests_by_semester(semester_id):
    """
    GET /admin/api/semesters/<semester_id>/contests
    Trả về tất cả contests thuộc một semester cụ thể.
    Query params:
      - state    : lọc theo state
      - page     : trang (default 1)
      - per_page : số item/trang (default 20, max 100)
    """
    sem = Semester.query.get_or_404(semester_id)

    state = request.args.get("state", "").strip()
    page = abs(request.args.get("page", 1, type=int))
    per_page = min(abs(request.args.get("per_page", 20, type=int)), 100)

    query = Contest.query.filter(Contest.semester_name == sem.semester_name)
    if state:
        query = query.filter(Contest.state == state)

    pagination = query.order_by(Contest.created_at.desc()).paginate(
        page=page, per_page=per_page, error_out=False
    )

    return jsonify({
        "success": True,
        "semester": {
            "id": sem.id,
            "semester_name": sem.semester_name,
            "start_time": sem.start_time.isoformat() if sem.start_time else None,
            "end_time": sem.end_time.isoformat() if sem.end_time else None,
        },
        "contests": [_contest_json(c) for c in pagination.items],
        "pagination": {
            "page": pagination.page,
            "per_page": per_page,
            "total": pagination.total,
            "pages": pagination.pages,
            "has_next": pagination.has_next,
            "has_prev": pagination.has_prev,
        },
    })


# ── Xem tất cả semesters kèm contests (JSON) ──────────────────────────────────

@admin.route("/admin/api/semesters/contests")
@admins_only
def api_all_semesters_with_contests():
    """
    GET /admin/api/semesters/contests
    Trả về tất cả semesters, mỗi semester kèm danh sách contests của nó.
    """
    semesters = Semester.query.order_by(Semester.id.desc()).all()

    result = []
    for sem in semesters:
        contests = (
            Contest.query
            .filter(Contest.semester_name == sem.semester_name)
            .order_by(Contest.created_at.desc())
            .all()
        )
        result.append({
            "id": sem.id,
            "semester_name": sem.semester_name,
            "start_time": sem.start_time.isoformat() if sem.start_time else None,
            "end_time": sem.end_time.isoformat() if sem.end_time else None,
            "contest_count": len(contests),
            "contests": [_contest_json(c) for c in contests],
        })

    # Contests không thuộc semester nào
    no_sem = (
        Contest.query
        .filter(db.or_(Contest.semester_name.is_(None), Contest.semester_name == ""))
        .order_by(Contest.created_at.desc())
        .all()
    )
    if no_sem:
        result.append({
            "id": None,
            "semester_name": None,
            "start_time": None,
            "end_time": None,
            "contest_count": len(no_sem),
            "contests": [_contest_json(c) for c in no_sem],
        })

    return jsonify({"success": True, "semesters": result})


# ── Create contest (JSON) ──────────────────────────────────────────────────────

@admin.route("/admin/api/contests", methods=["POST"])
@admins_only
def api_contest_create():
    """
    POST /admin/api/contests
    Body JSON:
      name (required), slug, description, semester_id,
      state, user_mode, start_time, end_time, freeze_scoreboard_at
    """
    data = request.get_json(silent=True) or {}

    name = (data.get("name") or "").strip()
    if not name:
        return jsonify({"success": False, "message": "name là bắt buộc."}), 400

    slug = _unique_slug(data.get("slug") or name)

    semester_name = None
    sem_id = data.get("semester_id")
    if sem_id:
        sem = Semester.query.get(sem_id)
        if not sem:
            return jsonify({"success": False, "message": f"Semester id={sem_id} không tồn tại."}), 404
        semester_name = sem.semester_name

    def _parse_iso(key):
        val = (data.get(key) or "").strip()
        if not val:
            return None
        try:
            return datetime.datetime.fromisoformat(val)
        except ValueError:
            return None

    from CTFd.utils.user import get_current_user
    owner = get_current_user()

    contest = Contest(
        name=name,
        description=(data.get("description") or "").strip() or None,
        slug=slug,
        semester_name=semester_name,
        owner_id=owner.id if owner else None,
        state=data.get("state", "hidden"),
        user_mode=data.get("user_mode", "users"),
        start_time=_parse_iso("start_time"),
        end_time=_parse_iso("end_time"),
        freeze_scoreboard_at=_parse_iso("freeze_scoreboard_at"),
    )
    db.session.add(contest)
    db.session.commit()

    return jsonify({"success": True, "contest": _contest_json(contest)}), 201


# ── Get / Update / Delete contest (JSON) ──────────────────────────────────────

@admin.route("/admin/api/contests/<int:contest_id>", methods=["GET"])
@admins_only
def api_contest_get(contest_id):
    """GET /admin/api/contests/<id>"""
    contest = Contest.query.get_or_404(contest_id)
    return jsonify({"success": True, "contest": _contest_json(contest)})


@admin.route("/admin/api/contests/<int:contest_id>", methods=["PUT", "PATCH"])
@admins_only
def api_contest_update(contest_id):
    """
    PUT/PATCH /admin/api/contests/<id>
    Body JSON: các field muốn cập nhật (name, slug, description, semester_id,
               state, user_mode, start_time, end_time, freeze_scoreboard_at)
    """
    contest = Contest.query.get_or_404(contest_id)
    data = request.get_json(silent=True) or {}

    if "name" in data:
        name = data["name"].strip()
        if not name:
            return jsonify({"success": False, "message": "name không được trống."}), 400
        contest.name = name

    if "slug" in data:
        new_slug = data["slug"].strip()
        if new_slug and new_slug != contest.slug:
            contest.slug = _unique_slug(new_slug, exclude_id=contest.id)

    if "description" in data:
        contest.description = (data["description"] or "").strip() or None

    if "state" in data:
        contest.state = data["state"]

    if "user_mode" in data:
        contest.user_mode = data["user_mode"]

    if "semester_id" in data:
        sem_id = data["semester_id"]
        if sem_id is None:
            contest.semester_name = None
        else:
            sem = Semester.query.get(sem_id)
            if not sem:
                return jsonify({"success": False, "message": f"Semester id={sem_id} không tồn tại."}), 404
            contest.semester_name = sem.semester_name

    def _parse_iso(key):
        val = (data.get(key) or "").strip()
        if not val:
            return None
        try:
            return datetime.datetime.fromisoformat(val)
        except ValueError:
            return None

    if "start_time" in data:
        contest.start_time = _parse_iso("start_time")
    if "end_time" in data:
        contest.end_time = _parse_iso("end_time")
    if "freeze_scoreboard_at" in data:
        contest.freeze_scoreboard_at = _parse_iso("freeze_scoreboard_at")

    contest.updated_at = datetime.datetime.utcnow()
    db.session.commit()

    return jsonify({"success": True, "contest": _contest_json(contest)})


@admin.route("/admin/api/contests/<int:contest_id>", methods=["DELETE"])
@admins_only
def api_contest_delete(contest_id):
    """DELETE /admin/api/contests/<id>"""
    contest = Contest.query.get_or_404(contest_id)
    name = contest.name
    db.session.delete(contest)
    db.session.commit()
    return jsonify({"success": True, "message": f"Đã xoá contest '{name}'."})
