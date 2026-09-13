#!/usr/bin/env python3
"""Exercise the real source checker with Windows cp950 as the implicit text encoding."""
# Monkey Mouse, modified 2026-09-13; GPL-2.0. No OS locale changes, native code or sockets.
import contextlib
import io
import json
from pathlib import Path
import runpy
from unittest.mock import patch

original_read = Path.read_text

def windows_default_read(path, *args, **kwargs):
    # Preserve an explicit source encoding, but reproduce the failing Windows default otherwise.
    if not args and kwargs.get("encoding") is None:
        kwargs["encoding"] = "cp950"
    return original_read(path, *args, **kwargs)

output = io.StringIO()
with patch.object(Path, "read_text", windows_default_read), contextlib.redirect_stdout(output):
    runpy.run_path(str(Path(__file__).with_name("check_source.py")), run_name="__main__")
checks = json.loads(output.getvalue())
assert checks and all(checks.values()), checks
print("Source checker passes with simulated Windows cp950 default: %d checks" % len(checks))
