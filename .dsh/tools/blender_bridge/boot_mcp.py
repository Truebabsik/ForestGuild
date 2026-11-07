"""Boot the MCP-for-Blender addon in a normal (GUI) Blender session.

Run as a startup script of the *GUI* Blender, not in --background: the addon refuses
to auto-start its socket server headless (addon.py, _blendermcp_ensure_server_running).

    blender.exe --python .dsh/tools/blender_bridge/boot_mcp.py

Enables the addon (and remembers it in the user preferences), so the next launches
start the server on their own, then reports the state the agent checks against.
"""

import addon_utils
import bpy

ADDON = "blender_mcp"

module = addon_utils.enable(ADDON, default_set=True, persistent=True)
enabled, _ = addon_utils.check(ADDON)
bpy.ops.wm.save_userpref()

scene = bpy.context.scene
print("[BOOT] addon=%s enabled=%s auto_start=%s port=%s background=%s"
      % (module, enabled, scene.blendermcp_auto_start_server, scene.blendermcp_port,
         bpy.app.background), flush=True)
