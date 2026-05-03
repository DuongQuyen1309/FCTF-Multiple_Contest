"""Fix contests_challenges column names to match SQLAlchemy model

Revision ID: f2704270001p
Revises: e2604240001p
Create Date: 2026-04-27 00:01:00.000000

=== MỤC ĐÍCH ===

  Migration e2604240001p đã đổi tên hai cột trong contests_challenges:
    max_attempts  → max_attempt   (bỏ 's')
    last_update   → updated_at

  Nhưng SQLAlchemy model ContestsChallenges vẫn dùng tên cũ:
    max_attempts  (có 's')
    last_update

  → Đổi lại tên cột về đúng với model để tránh OperationalError khi query.
"""

from alembic import op
import sqlalchemy as sa


# ---------------------------------------------------------------------------
# Revision identifiers
# ---------------------------------------------------------------------------
revision = "f2704270001p"
down_revision = "e2604240001p"
branch_labels = None
depends_on = None


def _has_column(bind, table: str, column: str) -> bool:
    if not sa.inspect(bind).has_table(table):
        return False
    return column in {c["name"] for c in sa.inspect(bind).get_columns(table)}


# ===========================================================================
# UPGRADE — đổi lại về tên model dùng
# ===========================================================================
def upgrade():
    bind = op.get_bind()

    # max_attempt → max_attempts (khôi phục 's')
    if _has_column(bind, "contests_challenges", "max_attempt") and \
       not _has_column(bind, "contests_challenges", "max_attempts"):
        op.execute(sa.text(
            "ALTER TABLE `contests_challenges` "
            "CHANGE COLUMN `max_attempt` `max_attempts` INT(11) NULL DEFAULT 0"
        ))

    # updated_at → last_update
    if _has_column(bind, "contests_challenges", "updated_at") and \
       not _has_column(bind, "contests_challenges", "last_update"):
        op.execute(sa.text(
            "ALTER TABLE `contests_challenges` "
            "CHANGE COLUMN `updated_at` `last_update` DATETIME(6) NULL"
        ))


# ===========================================================================
# DOWNGRADE — đổi lại về tên mà e2604240001p đã đặt
# ===========================================================================
def downgrade():
    bind = op.get_bind()

    # max_attempts → max_attempt
    if _has_column(bind, "contests_challenges", "max_attempts") and \
       not _has_column(bind, "contests_challenges", "max_attempt"):
        op.execute(sa.text(
            "ALTER TABLE `contests_challenges` "
            "CHANGE COLUMN `max_attempts` `max_attempt` INT(11) NULL DEFAULT 0"
        ))

    # last_update → updated_at
    if _has_column(bind, "contests_challenges", "last_update") and \
       not _has_column(bind, "contests_challenges", "updated_at"):
        op.execute(sa.text(
            "ALTER TABLE `contests_challenges` "
            "CHANGE COLUMN `last_update` `updated_at` DATETIME(6) NULL"
        ))
