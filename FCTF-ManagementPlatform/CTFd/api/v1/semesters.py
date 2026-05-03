"""
Semester management API.

Endpoints:
  GET/POST    /api/v1/semesters
  GET/PATCH/DELETE  /api/v1/semesters/<id>
  GET         /api/v1/semesters/<id>/contests
"""

import datetime

from flask import request
from flask_restx import Namespace, Resource

from CTFd.models import Contests, Semester, db
from CTFd.utils.decorators import admins_only
from CTFd.utils.user import is_admin

semesters_namespace = Namespace("semesters", description="Semester management endpoints")


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _semester_or_404(semester_id: int) -> Semester:
    s = Semester.query.get(semester_id)
    if s is None:
        semesters_namespace.abort(404, "Semester not found")
    return s


def _serialize_semester(s: Semester) -> dict:
    return {
        "id": s.id,
        "semester_name": s.semester_name,
        "start_time": s.start_time.isoformat() if s.start_time else None,
        "end_time": s.end_time.isoformat() if s.end_time else None,
        "contest_count": s.contests.count(),
    }


def _serialize_contest(c: Contests) -> dict:
    return {
        "id": c.id,
        "name": c.name,
        "description": c.description,
        "slug": c.slug,
        "owner_id": c.owner_id,
        "semester_name": c.semester_name,
        "state": c.state,
        "user_mode": c.user_mode,
        "start_time": c.start_time.isoformat() if c.start_time else None,
        "end_time": c.end_time.isoformat() if c.end_time else None,
        "freeze_scoreboard_at": c.freeze_scoreboard_at.isoformat() if c.freeze_scoreboard_at else None,
        "created_at": c.created_at.isoformat() if c.created_at else None,
        "participant_count": c.participants.count(),
        "challenge_count": c.challenges.count(),
    }


def _parse_dt(val):
    if not val:
        return None
    for fmt in ("%Y-%m-%dT%H:%M:%S", "%Y-%m-%d %H:%M:%S", "%Y-%m-%d"):
        try:
            return datetime.datetime.strptime(val, fmt)
        except ValueError:
            continue
    return None


# ---------------------------------------------------------------------------
# Semester list / create
# ---------------------------------------------------------------------------

@semesters_namespace.route("")
class SemesterList(Resource):
    method_decorators = [admins_only]

    def get(self):
        """List all semesters ordered by start_time descending."""
        semesters = Semester.query.order_by(Semester.id.desc()).all()
        return {"success": True, "data": [_serialize_semester(s) for s in semesters]}

    def post(self):
        """Create a new semester.

        Body (JSON):
          semester_name  — required, unique
          start_time?    — ISO date string
          end_time?      — ISO date string
        """
        data = request.get_json(silent=True) or {}

        semester_name = (data.get("semester_name") or "").strip()
        if not semester_name:
            semesters_namespace.abort(400, "semester_name is required")

        if Semester.query.filter_by(semester_name=semester_name).first():
            semesters_namespace.abort(400, f"Semester '{semester_name}' already exists")

        s = Semester(
            semester_name=semester_name,
            start_time=_parse_dt(data.get("start_time")),
            end_time=_parse_dt(data.get("end_time")),
        )
        db.session.add(s)
        db.session.commit()
        return {"success": True, "data": _serialize_semester(s)}, 201


# ---------------------------------------------------------------------------
# Semester detail / update / delete
# ---------------------------------------------------------------------------

@semesters_namespace.route("/<int:semester_id>")
class SemesterDetail(Resource):
    method_decorators = [admins_only]

    def get(self, semester_id):
        s = _semester_or_404(semester_id)
        return {"success": True, "data": _serialize_semester(s)}

    def patch(self, semester_id):
        """Partial update of a semester."""
        s = _semester_or_404(semester_id)
        data = request.get_json(silent=True) or {}

        if "semester_name" in data:
            new_name = (data["semester_name"] or "").strip()
            if not new_name:
                semesters_namespace.abort(400, "semester_name cannot be empty")
            existing = Semester.query.filter_by(semester_name=new_name).first()
            if existing and existing.id != semester_id:
                semesters_namespace.abort(400, f"Semester name '{new_name}' already exists")
            s.semester_name = new_name

        if "start_time" in data:
            s.start_time = _parse_dt(data["start_time"])
        if "end_time" in data:
            s.end_time = _parse_dt(data["end_time"])

        db.session.commit()
        return {"success": True, "data": _serialize_semester(s)}

    def delete(self, semester_id):
        """Delete a semester (and cascade-deletes its contests)."""
        s = _semester_or_404(semester_id)
        db.session.delete(s)
        db.session.commit()
        return {"success": True}


# ---------------------------------------------------------------------------
# Contests inside a semester
# ---------------------------------------------------------------------------

@semesters_namespace.route("/<int:semester_id>/contests")
class SemesterContestList(Resource):
    method_decorators = [admins_only]

    def get(self, semester_id):
        """List all contests belonging to this semester."""
        s = _semester_or_404(semester_id)
        contests = (
            Contests.query
            .filter_by(semester_name=s.semester_name)
            .order_by(Contests.id.desc())
            .all()
        )
        return {"success": True, "data": [_serialize_contest(c) for c in contests]}
