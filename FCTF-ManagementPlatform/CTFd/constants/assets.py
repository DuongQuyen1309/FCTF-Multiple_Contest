import logging
import os

from flask import current_app, url_for

from CTFd.utils import get_asset_json
from CTFd.utils.config import ctf_theme
from CTFd.utils.helpers import markup

logger = logging.getLogger(__name__)


class _AssetsWrapper:
    def manifest(self, theme=None, _return_none_on_load_failure=False):
        if theme is None:
            theme = ctf_theme()
        file_path = os.path.join(
            current_app.root_path, "themes", theme, "static", "manifest.json"
        )

        try:
            manifest = get_asset_json(path=file_path)
        except FileNotFoundError as e:
            # This check allows us to determine if we are on a legacy theme and fallback if necessary
            if _return_none_on_load_failure:
                manifest = None
            else:
                logger.warning(
                    "manifest.json not found at %s. "
                    "Run 'npm install && npm run build' inside CTFd/themes/%s to generate static assets.",
                    file_path,
                    theme,
                )
                raise e
        return manifest

    def js(self, asset_key, theme=None, defer=True):
        if theme is None:
            theme = ctf_theme()
        try:
            manifest = self.manifest(theme=theme)
            if manifest is None or asset_key not in manifest:
                return markup("")
            asset = manifest[asset_key]
            entry = asset["file"]
            imports = asset.get("imports", [])
            extra_attr = "defer " if defer else ""
            html = ""
            for i in imports:
                # TODO: Needs a better recursive solution
                i = manifest[i]["file"]
                url = url_for("views.themes_beta", theme=theme, path=i)
                html += f'<script {extra_attr}type="module" src="{url}"></script>'
            url = url_for("views.themes_beta", theme=theme, path=entry)
            html += f'<script {extra_attr}type="module" src="{url}"></script>'
            return markup(html)
        except (FileNotFoundError, KeyError, TypeError) as e:
            logger.warning("Assets.js(%s, theme=%s) failed: %s", asset_key, theme, e)
            return markup("")

    def css(self, asset_key, theme=None):
        if theme is None:
            theme = ctf_theme()
        try:
            manifest = self.manifest(theme=theme)
            if manifest is None or asset_key not in manifest:
                return markup("")
            asset = manifest[asset_key]
            entry = asset["file"]
            url = url_for("views.themes_beta", theme=theme, path=entry)
            return markup(f'<link rel="stylesheet" href="{url}">')
        except (FileNotFoundError, KeyError, TypeError) as e:
            logger.warning("Assets.css(%s, theme=%s) failed: %s", asset_key, theme, e)
            return markup("")

    def file(self, asset_key, theme=None):
        if theme is None:
            theme = ctf_theme()
        try:
            manifest = self.manifest(theme=theme)
            if manifest is None or asset_key not in manifest:
                return ""
            asset = manifest[asset_key]
            entry = asset["file"]
            return url_for("views.themes_beta", theme=theme, path=entry)
        except (FileNotFoundError, KeyError, TypeError) as e:
            logger.warning("Assets.file(%s, theme=%s) failed: %s", asset_key, theme, e)
            return ""


Assets = _AssetsWrapper()
