"""Send one command to the running Blender, over the MCP-for-Blender addon's socket.

The addon (installed as ``blender_mcp``) listens on localhost:9876 inside a *GUI* Blender
and executes commands on its main thread. This is exactly the channel the MCP server uses,
so anything the agent does here is also what the MCP tools would do — and the human sees
every change in the Blender window while it happens.

    python bpy_call.py --ping                  # is Blender there
    python bpy_call.py --info                  # scene summary (get_scene_info, no code)
    python bpy_call.py --shot out.png          # viewport screenshot
    python bpy_call.py --code pose.py          # run pose.py inside Blender (execute_code)
    python bpy_call.py --cmd raw.json          # any {"type": ..., "params": {...}}

The reply arrives as JSON on stdout. A non-"success" status is printed and the process
exits 1, so a caller can see the failure without parsing.
"""

import argparse
import json
import os
import socket
import sys

HOST = os.environ.get("BLENDER_MCP_HOST", "127.0.0.1")
PORT = int(os.environ.get("BLENDER_MCP_PORT", "9876"))


def call(command, timeout):
    with socket.create_connection((HOST, PORT), timeout=timeout) as sock:
        sock.settimeout(timeout)
        sock.sendall(json.dumps(command).encode("utf-8"))
        chunks = []
        while True:
            data = sock.recv(1 << 20)
            if not data:
                break
            chunks.append(data)
            try:                                   # replies are one JSON document
                return json.loads(b"".join(chunks).decode("utf-8"))
            except (json.JSONDecodeError, UnicodeDecodeError):
                continue                           # split across recv chunks: keep reading
    raise SystemExit("no reply from Blender at %s:%d" % (HOST, PORT))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default=HOST)
    ap.add_argument("--port", default=PORT, type=int)
    ap.add_argument("--timeout", default=1800.0, type=float)
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--ping", action="store_true")
    mode.add_argument("--info", action="store_true")
    mode.add_argument("--shot", metavar="PNG")
    mode.add_argument("--code", metavar="PY")
    mode.add_argument("--cmd", metavar="JSON")
    args = ap.parse_args()

    globals()["HOST"], globals()["PORT"] = args.host, args.port
    if args.ping:
        command = {"type": "ping", "params": {}}
    elif args.info:
        command = {"type": "get_scene_info", "params": {}}
    elif args.shot:
        command = {"type": "get_viewport_screenshot",
                   "params": {"filepath": os.path.abspath(args.shot), "max_size": 1600}}
    elif args.code:
        with open(args.code, "r", encoding="utf-8") as fh:
            command = {"type": "execute_code", "params": {"code": fh.read()}}
    else:
        with open(args.cmd, "r", encoding="utf-8") as fh:
            command = json.load(fh)

    reply = call(command, args.timeout)
    print(json.dumps(reply, ensure_ascii=False, indent=2))
    if reply.get("status") != "success":
        sys.exit(1)


if __name__ == "__main__":
    main()
