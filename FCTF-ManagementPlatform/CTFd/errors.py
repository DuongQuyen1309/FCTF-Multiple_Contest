import traceback

import jinja2.exceptions
from flask import render_template, request
from werkzeug.exceptions import InternalServerError


def render_error(error):
    # ----------------------------------------------------------------
    # Hiển thị full traceback cho lỗi 500 để debug nhanh trên production
    # ⚠️  Xóa block này khi không cần debug nữa
    # ----------------------------------------------------------------
    if isinstance(error, InternalServerError):
        original = getattr(error, "original_exception", None)
        tb_text = traceback.format_exc() if original else "(không có traceback)"
        html = f"""<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="utf-8">
  <title>500 – Internal Server Error (DEBUG)</title>
  <style>
    * {{ box-sizing: border-box; margin: 0; padding: 0; }}
    body {{ background: #0f0f11; color: #e2e8f0; font-family: 'Segoe UI', system-ui, monospace; padding: 2rem; }}
    h1 {{ color: #f87171; font-size: 1.6rem; margin-bottom: .5rem; }}
    .meta {{ color: #94a3b8; font-size: .85rem; margin-bottom: 1.5rem; }}
    .card {{ background: #1e1e2e; border: 1px solid #374151; border-radius: 10px; padding: 1.5rem; margin-bottom: 1.5rem; }}
    .card h2 {{ color: #60a5fa; font-size: 1rem; margin-bottom: .75rem; text-transform: uppercase; letter-spacing: .05em; }}
    pre {{ white-space: pre-wrap; word-break: break-word; font-size: .82rem; line-height: 1.6; color: #fca5a5; }}
    .exc-type {{ color: #f472b6; font-weight: bold; }}
    .path {{ color: #a3e635; }}
    .badge {{ display: inline-block; background: #7c3aed; color: #fff; font-size: .7rem;
              padding: 2px 8px; border-radius: 999px; margin-left: .5rem; vertical-align: middle; }}
  </style>
</head>
<body>
  <h1>⚠️ 500 – Internal Server Error <span class="badge">DEBUG MODE</span></h1>
  <div class="meta">
    <span class="path">{request.method} {request.path}</span>
  </div>

  <div class="card">
    <h2>Exception</h2>
    <pre><span class="exc-type">{type(original).__name__ if original else 'UnknownException'}</span>: {original}</pre>
  </div>

  <div class="card">
    <h2>Traceback</h2>
    <pre>{tb_text}</pre>
  </div>
</body>
</html>"""
        return html, 500
    # ----------------------------------------------------------------

    if (
        hasattr(error, "description")
        and isinstance(error, InternalServerError)
        and error.description == InternalServerError.description
    ):
        error.description = "An Internal Server Error has occurred"
    try:
        return (
            render_template(
                "errors/{}.html".format(error.code),
                error=error.description,
            ),
            error.code,
        )
    except jinja2.exceptions.TemplateNotFound:
        return error.get_response()
