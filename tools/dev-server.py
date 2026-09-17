"""Static file server for the prototype, with caching disabled.

Python's stock http.server lets browsers heuristically cache JS and CSS, which
means edits silently do not show up. That wastes more time than it saves, so
every response here is marked no-store.

    python tools/dev-server.py [port] [directory]
"""
import sys
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer


class NoCacheHandler(SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Cache-Control", "no-store, must-revalidate")
        self.send_header("Pragma", "no-cache")
        self.send_header("Expires", "0")
        super().end_headers()

    def log_message(self, fmt, *args):
        # Keep the console readable: errors only.
        if not args or not str(args[1]).startswith("2"):
            super().log_message(fmt, *args)


def main() -> None:
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 4173
    directory = sys.argv[2] if len(sys.argv) > 2 else "prototype"
    handler = partial(NoCacheHandler, directory=directory)
    with ThreadingHTTPServer(("127.0.0.1", port), handler) as httpd:
        print(f"Just Paints prototype on http://localhost:{port} (serving {directory}/, no cache)")
        httpd.serve_forever()


if __name__ == "__main__":
    main()
