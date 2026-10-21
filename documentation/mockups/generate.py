"""Generator wireframov pre sekciu 6 wiki (Návrh view aplikácie).

Spustenie:  python generate.py          -> vytvorí *.svg v tomto priečinku
PNG:        render.sh (headless Chrome)  -> vytvorí *.png zo *.svg
"""
from pathlib import Path
from xml.sax.saxutils import escape

W, H = 1280, 800
TOP, SIDE = 56, 200
CX = SIDE + 24          # ľavý okraj obsahu
CW = W - CX - 24        # šírka obsahu

PRIMARY = "#1F4E79"
BORDER = "#C9D1DB"
MUTED = "#6B7785"
TEXT = "#1E2A36"
BG = "#FFFFFF"
PANEL = "#F3F5F8"

# farby stavov misie a robota (bg, text)
BADGE = {
    "Created": ("#ECEFF3", "#4A5563"),
    "Submitted": ("#E3ECFA", "#2B5BA8"),
    "InProgress": ("#DCEBFF", "#1F4E79"),
    "Cancelling": ("#FFF1D6", "#8A5A00"),
    "Completed": ("#DDF3E4", "#1E6B3A"),
    "Cancelled": ("#ECEFF3", "#4A5563"),
    "Failed": ("#FBE0E0", "#9B1C1C"),
    "Dostupný": ("#DDF3E4", "#1E6B3A"),
    "Zaneprázdnený": ("#DCEBFF", "#1F4E79"),
    "Offline": ("#ECEFF3", "#4A5563"),
}


class Svg:
    def __init__(self):
        self.parts = []

    def add(self, s):
        self.parts.append(s)

    def rect(self, x, y, w, h, fill=BG, stroke=BORDER, rx=4, sw=1, extra=""):
        self.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}" '
                 f'fill="{fill}" stroke="{stroke}" stroke-width="{sw}" {extra}/>')

    def line(self, x1, y1, x2, y2, stroke=BORDER, sw=1, dash=None):
        d = f' stroke-dasharray="{dash}"' if dash else ""
        self.add(f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{stroke}" stroke-width="{sw}"{d}/>')

    def text(self, x, y, s, size=13, fill=TEXT, weight="normal", anchor="start", italic=False):
        st = ' font-style="italic"' if italic else ""
        self.add(f'<text x="{x}" y="{y}" font-size="{size}" fill="{fill}" font-weight="{weight}" '
                 f'text-anchor="{anchor}"{st}>{escape(s)}</text>')

    def circle(self, x, y, r, fill):
        self.add(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"/>')

    # --- komponenty -------------------------------------------------------

    def badge(self, x, y, label, size=12):
        bg, fg = BADGE.get(label, ("#ECEFF3", "#4A5563"))
        w = int(len(label) * size * 0.6) + 18
        self.rect(x, y - 14, w, 20, fill=bg, stroke=bg, rx=10)
        self.text(x + w / 2, y + 1, label, size=size, fill=fg, weight="600", anchor="middle")
        return w

    def button(self, x, y, w, label, kind="primary", h=34):
        if kind == "primary":
            self.rect(x, y, w, h, fill=PRIMARY, stroke=PRIMARY, rx=5)
            col = "#FFFFFF"
        elif kind == "danger":
            self.rect(x, y, w, h, fill="#FFFFFF", stroke="#B42318", rx=5)
            col = "#B42318"
        elif kind == "danger-solid":
            self.rect(x, y, w, h, fill="#B42318", stroke="#B42318", rx=5)
            col = "#FFFFFF"
        else:
            self.rect(x, y, w, h, fill="#FFFFFF", stroke=BORDER, rx=5)
            col = TEXT
        self.text(x + w / 2, y + h / 2 + 5, label, size=13, fill=col, weight="600", anchor="middle")

    def small_button(self, x, y, w, label, kind="secondary"):
        self.button(x, y, w, label, kind, h=26)

    def input(self, x, y, w, label, value, placeholder=False, select=False, error=None, h=34):
        self.text(x, y - 8, label, size=12, fill=MUTED, weight="600")
        self.rect(x, y, w, h, stroke="#B42318" if error else BORDER)
        self.text(x + 10, y + h / 2 + 5, value, size=13, fill=MUTED if placeholder else TEXT,
                  italic=placeholder)
        if select:
            ax, ay = x + w - 18, y + h / 2 - 2
            self.add(f'<path d="M{ax} {ay} l5 5 l5 -5" fill="none" stroke="{MUTED}" stroke-width="1.5"/>')
        if error:
            self.text(x, y + h + 15, error, size=11, fill="#B42318")

    def card(self, x, y, w, h, title=None):
        self.rect(x, y, w, h, rx=6)
        if title:
            self.text(x + 16, y + 26, title, size=14, weight="700")
            self.line(x, y + 40, x + w, y + 40)

    def note(self, x, y, lines, w=None):
        """Žltá poznámka pre vývojárov (nie je súčasťou UI)."""
        w = w or max(len(l) for l in lines) * 6.4 + 24
        h = 18 * len(lines) + 14
        self.rect(x, y, w, h, fill="#FFF8C5", stroke="#D4B106", rx=3, extra='stroke-dasharray="4 3"')
        for i, l in enumerate(lines):
            self.text(x + 10, y + 21 + i * 18, l, size=11.5, fill="#5C4A00")

    # --- spoločný layout ---------------------------------------------------

    def chrome(self, active, breadcrumb):
        self.rect(0, 0, W, H, fill=BG, stroke="none", rx=0)
        # top bar
        self.rect(0, 0, W, TOP, fill=PRIMARY, stroke="none", rx=0)
        self.rect(20, 16, 24, 24, fill="#FFFFFF", stroke="none", rx=5)
        self.text(32, 33, "ITS", size=9, fill=PRIMARY, weight="800", anchor="middle")
        self.text(56, 34, "ITS Middleware", size=17, fill="#FFFFFF", weight="700")
        self.text(188, 34, "KUKA AMR Fleet Manager", size=13, fill="#B9CDE2")
        # stav spojenia / sync / používateľ
        self.rect(800, 15, 168, 26, fill="#2C628F", stroke="none", rx=13)
        self.circle(816, 28, 5, "#4ADE80")
        self.text(828, 33, "KUKA FM pripojené", size=12, fill="#FFFFFF")
        self.rect(978, 15, 150, 26, fill="#2C628F", stroke="none", rx=13)
        self.text(1053, 33, "⟳ sync pred 3 s", size=12, fill="#FFFFFF", anchor="middle")
        self.circle(1158, 28, 14, "#FFFFFF")
        self.text(1158, 33, "OP", size=11, fill=PRIMARY, weight="700", anchor="middle")
        self.text(1180, 33, "Operátor", size=12, fill="#FFFFFF")

        # sidebar
        self.rect(0, TOP, SIDE, H - TOP, fill=PANEL, stroke="none", rx=0)
        self.line(SIDE, TOP, SIDE, H)
        items = [("misie", "▤", "Misie"), ("nova", "+", "Nová misia"), ("roboty", "◉", "Roboty")]
        y = TOP + 24
        self.text(20, y, "NAVIGÁCIA", size=10.5, fill=MUTED, weight="700")
        y += 14
        for key, icon, label in items:
            if key == active:
                self.rect(10, y, SIDE - 20, 36, fill="#DCE6F1", stroke="none", rx=5)
                self.rect(10, y, 4, 36, fill=PRIMARY, stroke="none", rx=2)
            col = PRIMARY if key == active else TEXT
            self.text(30, y + 23, icon, size=14, fill=col, weight="700", anchor="middle")
            self.text(46, y + 23, label, size=14, fill=col, weight="700" if key == active else "normal")
            y += 42
        self.line(16, y + 6, SIDE - 16, y + 6)
        self.text(30, y + 34, "≡", size=14, fill="#A0AAB6", anchor="middle")
        self.text(46, y + 34, "Audit log", size=14, fill="#A0AAB6")
        self.text(46, y + 50, "(voliteľné, neskôr)", size=10.5, fill="#A0AAB6", italic=True)
        self.text(20, H - 20, "v0.1 · build #42", size=10.5, fill=MUTED)

        # breadcrumb
        self.text(CX, TOP + 28, breadcrumb, size=12, fill=MUTED)

    def save(self, path):
        body = "\n".join(self.parts)
        svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}" '
               f'font-family="Segoe UI, Helvetica, Arial, sans-serif">\n{body}\n</svg>\n')
        Path(path).write_text(svg, encoding="utf-8")


# =========================================================================
# 6.1 Zoznam misií
# =========================================================================
MISSIONS = [
    ("M-2026-0142", "ERP-88121", "RACK_MOVE", "KMP-01", "2", "InProgress", "14.10. 13:40:12", True),
    ("M-2026-0141", "ERP-88120", "RACK_MOVE", "KMP-03", "1", "Submitted", "14.10. 13:38:55", True),
    ("M-2026-0140", "—", "ROLLER_MOVE", "KMP-02", "3", "Cancelling", "14.10. 13:31:07", True),
    ("M-2026-0139", "ERP-88117", "RACK_MOVE", "KMP-04", "2", "Completed", "14.10. 13:12:40", False),
    ("M-2026-0138", "ERP-88115", "RACK_MOVE", "—", "2", "Failed", "14.10. 13:05:22", False),
    ("M-2026-0137", "—", "ROLLER_MOVE", "KMP-01", "3", "Cancelled", "14.10. 12:58:03", False),
    ("M-2026-0136", "ERP-88110", "RACK_MOVE", "KMP-02", "1", "Completed", "14.10. 12:40:51", False),
]


def missions_list(out):
    s = Svg()
    s.chrome("misie", "Misie")
    y0 = TOP + 62
    s.text(CX, y0, "Misie", size=24, weight="700")
    s.button(W - 24 - 140, y0 - 26, 140, "+ Nová misia")

    # záložky
    ty = y0 + 26
    tabs = [("Aktívne", "3"), ("Ukončené", "4"), ("Všetky", "7")]
    tx = CX
    for i, (label, n) in enumerate(tabs):
        w = 112
        sel = label == "Všetky"
        s.text(tx + 12, ty + 22, label, size=14, fill=PRIMARY if sel else MUTED, weight="700" if sel else "normal")
        s.rect(tx + 14 + len(label) * 8.2, ty + 9, 24, 18, fill="#DCE6F1" if sel else "#ECEFF3", stroke="none", rx=9)
        s.text(tx + 26 + len(label) * 8.2, ty + 22, n, size=11, fill=PRIMARY if sel else MUTED, weight="700", anchor="middle")
        if sel:
            s.rect(tx, ty + 34, w, 3, fill=PRIMARY, stroke="none", rx=1)
        tx += w + 6
    s.line(CX, ty + 37, CX + CW, ty + 37)

    # filtre
    fy = ty + 52
    s.rect(CX, fy, 320, 34)
    s.text(CX + 12, fy + 22, "⌕  Hľadať MissionCode / ExternalRef…", size=13, fill=MUTED, italic=True)
    s.input(CX + 334, fy, 170, "", "Stav: všetky", select=True)
    s.input(CX + 516, fy, 170, "", "Robot: všetky", select=True)
    s.input(CX + 698, fy, 170, "", "Dnes", select=True)
    # auto-refresh
    s.rect(CX + CW - 150, fy + 4, 150, 26, fill="#F0F7F2", stroke="#B7DFC4", rx=13)
    s.circle(CX + CW - 136, fy + 17, 4, "#22A559")
    s.text(CX + CW - 126, fy + 22, "Live · obnova 5 s", size=12, fill="#1E6B3A")

    # tabuľka
    tyy = fy + 52
    cols = [("MissionCode", 0), ("ExternalRef", 120), ("Typ", 220), ("Robot", 336), ("Prio", 406),
            ("Stav", 456), ("Vytvorená", 580), ("Posl. sync", 714), ("Akcie", 830)]
    rh = 46
    s.rect(CX, tyy, CW, 38 + rh * len(MISSIONS), rx=6)
    s.rect(CX + 1, tyy + 1, CW - 2, 37, fill=PANEL, stroke="none", rx=5)
    s.line(CX, tyy + 38, CX + CW, tyy + 38)
    for name, dx in cols:
        s.text(CX + 16 + dx, tyy + 24, name.upper(), size=11, fill=MUTED, weight="700")
    s.text(CX + 16 + 580 + 74, tyy + 24, "↓", size=11, fill=MUTED, weight="700")

    for i, (code, ext, typ, robot, prio, status, created, active) in enumerate(MISSIONS):
        ry = tyy + 38 + i * rh
        if i == 0:
            s.rect(CX + 1, ry, CW - 2, rh, fill="#F7FAFD", stroke="none", rx=0)
        if i:
            s.line(CX, ry, CX + CW, ry, stroke="#E4E8EE")
        by = ry + rh / 2 + 5
        s.text(CX + 16, by, code, size=13, fill=PRIMARY, weight="600")
        s.text(CX + 16 + 120, by, ext, size=13, fill=TEXT if ext != "—" else MUTED)
        s.text(CX + 16 + 220, by, typ, size=12.5, fill=TEXT)
        s.text(CX + 16 + 336, by, robot, size=13, fill=TEXT if robot != "—" else MUTED)
        s.text(CX + 16 + 416, by, prio, size=13, anchor="middle")
        s.badge(CX + 16 + 456, by - 1, status)
        s.text(CX + 16 + 580, by, created, size=12.5, fill=TEXT)
        s.text(CX + 16 + 714, by, "pred 2 s" if active else "—", size=12.5, fill=MUTED)
        ax = CX + 16 + 830
        s.small_button(ax, ry + 10, 64, "Detail")
        if active and status != "Cancelling":
            s.small_button(ax + 72, ry + 10, 70, "Zrušiť", "danger")
        elif status == "Cancelling":
            s.text(ax + 76, by, "rušenie…", size=12, fill="#8A5A00", italic=True)

    # stránkovanie
    py = tyy + 38 + rh * len(MISSIONS) + 24
    s.text(CX, py, "Zobrazené 1–7 zo 7", size=12, fill=MUTED)
    for j, lab in enumerate(["‹", "1", "›"]):
        s.rect(CX + CW - 96 + j * 32, py - 18, 28, 26, fill=PRIMARY if lab == "1" else BG,
               stroke=PRIMARY if lab == "1" else BORDER, rx=4)
        s.text(CX + CW - 82 + j * 32, py, lab, size=12, fill="#FFF" if lab == "1" else TEXT, anchor="middle")

    s.note(CX, H - 58, ["Dáta z DB middlewaru (GET /api/missions), KUKA FM sa nevolá. Aktívne riadky sa obnovujú",
                        "pollingom každých 5 s. „Zrušiť“ len pre Submitted/InProgress → potvrdzovací dialóg (6.4)."])
    s.save(out)


# =========================================================================
# 6.2 Nová misia
# =========================================================================
def mission_new(out):
    s = Svg()
    s.chrome("nova", "Misie  /  Nová misia")
    y0 = TOP + 62
    s.text(CX, y0, "Nová misia", size=24, weight="700")
    s.text(CX, y0 + 22, "Misia sa uloží do DB a odošle na KUKA FM (submitMission).", size=13, fill=MUTED)

    lw = 600
    cy = y0 + 44
    s.card(CX, cy, lw, 206, "Parametre misie")
    fx = CX + 16
    s.input(fx, cy + 76, 276, "Typ misie *", "RACK_MOVE", select=True)
    s.input(fx + 292, cy + 76, 276, "Priorita *", "2 – normálna", select=True)
    s.input(fx, cy + 150, 276, "Robot", "Automaticky (určí KUKA FM)", select=True)
    s.input(fx + 292, cy + 150, 276, "ExternalRef", "napr. číslo objednávky z ERP", placeholder=True)

    # kroky
    sy = cy + 224
    s.card(CX, sy, lw, 280, "Cieľové body (kroky)")
    s.text(CX + lw - 16, sy + 26, "min. 1 krok", size=12, fill=MUTED, anchor="end")
    hy = sy + 52
    s.text(CX + 16, hy + 12, "#", size=11, fill=MUTED, weight="700")
    s.text(CX + 52, hy + 12, "UZOL (NodeCode)", size=11, fill=MUTED, weight="700")
    s.text(CX + 312, hy + 12, "AKCIA", size=11, fill=MUTED, weight="700")
    steps = [("1", "A-01-PICK", "Naložiť", None), ("2", "B-07-DROP", "Vyložiť", None),
             ("3", "", "Vybrať akciu", "Uzol je povinný")]
    for i, (n, node, act, err) in enumerate(steps):
        ry = hy + 24 + i * 50
        s.text(CX + 26, ry + 22, "⋮⋮", size=12, fill="#A0AAB6", anchor="middle")
        s.text(CX + 38, ry + 22, n, size=13, weight="700")
        s.rect(CX + 52, ry, 244, 34, stroke="#B42318" if err else BORDER)
        s.text(CX + 62, ry + 22, node or "Zadajte uzol…", size=13, fill=TEXT if node else MUTED, italic=not node)
        if err:
            s.text(CX + 52, ry + 47, err, size=11, fill="#B42318")
        s.rect(CX + 312, ry, 200, 34)
        s.text(CX + 322, ry + 22, act, size=13, fill=TEXT if node else MUTED)
        ax, ay = CX + 312 + 182, ry + 15
        s.add(f'<path d="M{ax} {ay} l5 5 l5 -5" fill="none" stroke="{MUTED}" stroke-width="1.5"/>')
        s.rect(CX + 528, ry + 2, 30, 30, stroke=BORDER, rx=4)
        s.text(CX + 543, ry + 22, "✕", size=13, fill=MUTED, anchor="middle")
    s.text(CX + 16, sy + 262, "+ Pridať krok", size=13, fill=PRIMARY, weight="700")

    # pravý panel – súhrn
    rx = CX + lw + 24
    rw = CW - lw - 24
    s.card(rx, cy, rw, 230, "Súhrn")
    rows = [("Typ", "RACK_MOVE"), ("Priorita", "2"), ("Robot", "automaticky"), ("Počet krokov", "3"),
            ("Trasa", "A-01-PICK → B-07-DROP → ?")]
    for i, (k, v) in enumerate(rows):
        s.text(rx + 16, cy + 70 + i * 30, k, size=13, fill=MUTED)
        s.text(rx + 140, cy + 70 + i * 30, v, size=13, weight="600")

    s.rect(rx, cy + 248, rw, 112, fill="#FDECEC", stroke="#F3B4B4", rx=6)
    s.text(rx + 16, cy + 274, "⚠  Formulár obsahuje chyby", size=13, fill="#9B1C1C", weight="700")
    s.text(rx + 16, cy + 298, "• Krok 3: uzol je povinný", size=12.5, fill="#9B1C1C")
    s.text(rx + 16, cy + 326, "Chyby z API (400 / 502) sa zobrazia tu,", size=12, fill="#7A2E2E")
    s.text(rx + 16, cy + 344, "napr. „KUKA FM nedostupné – skúste znova“.", size=12, fill="#7A2E2E")

    s.button(rx, cy + 378, (rw - 12) // 2, "Zrušiť", "secondary", h=40)
    s.button(rx + (rw - 12) // 2 + 12, cy + 378, (rw - 12) // 2, "Odoslať misiu", h=40)
    s.text(rx + rw / 2, cy + 440, "Po úspechu → presmerovanie na detail misie", size=12, fill=MUTED, anchor="middle")

    s.note(rx, H - 96, ["POST /api/missions → 201 + Id misie.",
                        "Klientska validácia + serverová (400).",
                        "Číselníky (typy, roboty) z GET /api/robots…"], w=rw)
    s.save(out)


# =========================================================================
# 6.3 Detail misie (+ 6.4 dialóg zrušenia)
# =========================================================================
def mission_detail(out, with_dialog=False):
    s = Svg()
    s.chrome("misie", "Misie  /  M-2026-0142")
    y0 = TOP + 62
    s.text(CX, y0, "M-2026-0142", size=24, weight="700")
    s.badge(CX + 168, y0 - 7, "InProgress", size=13)
    s.text(CX, y0 + 22, "ExternalRef ERP-88121 · RACK_MOVE · vytvoril: web-ui", size=13, fill=MUTED)
    s.button(W - 24 - 128, y0 - 26, 128, "Zrušiť misiu", "danger")
    s.button(W - 24 - 128 - 12 - 92, y0 - 26, 92, "⟳ Obnoviť", "secondary")

    lw = 560
    cy = y0 + 44
    s.card(CX, cy, lw, 222, "Informácie")
    info = [("Robot", "KMP-01"), ("Priorita", "2"), ("Vytvorená", "14.10.2026 13:40:12"),
            ("Odoslaná (Submitted)", "14.10.2026 13:40:13"), ("Ukončená", "—"),
            ("Posledná synchronizácia", "pred 2 s"), ("Chyba", "—")]
    for i, (k, v) in enumerate(info):
        col = i % 2
        row = i // 2
        x = CX + 16 + col * 272
        y = cy + 66 + row * 42
        s.text(x, y, k, size=11.5, fill=MUTED, weight="600")
        s.text(x, y + 18, v, size=13.5, fill=PRIMARY if k == "Robot" else TEXT, weight="600")

    sy = cy + 240
    s.card(CX, sy, lw, 200, "Kroky misie")
    steps = [("1", "A-01-PICK", "Naložiť", "done"), ("2", "C-03-WAIT", "Čakať", "current"),
             ("3", "B-07-DROP", "Vyložiť", "todo")]
    for i, (n, node, act, st) in enumerate(steps):
        y = sy + 72 + i * 42
        fill = {"done": "#22A559", "current": PRIMARY, "todo": "#FFFFFF"}[st]
        if i < len(steps) - 1:
            s.line(CX + 30, y + 10, CX + 30, y + 32, stroke="#C9D1DB", sw=2)
        s.add(f'<circle cx="{CX + 30}" cy="{y - 4}" r="10" fill="{fill}" stroke="{PRIMARY if st != "done" else fill}" stroke-width="1.5"/>')
        s.text(CX + 30, y + 1, "✓" if st == "done" else n, size=11, fill="#FFF" if st != "todo" else PRIMARY,
               weight="700", anchor="middle")
        s.text(CX + 52, y + 1, node, size=13.5, weight="600")
        s.text(CX + 180, y + 1, act, size=13, fill=MUTED)
        if st == "current":
            s.text(CX + lw - 16, y + 1, "robot je tu", size=12, fill=PRIMARY, anchor="end", italic=True)
    s.text(CX + 16, sy + 188, "Priebeh krokov – len ak ho Query jobs API poskytne (inak len zoznam).", size=11,
           fill=MUTED, italic=True)

    # história stavov
    rx = CX + lw + 24
    rw = CW - lw - 24
    s.card(rx, cy, rw, 440, "História stavov")
    s.text(rx + rw - 16, cy + 26, "MissionStatusHistory", size=11, fill=MUTED, anchor="end")
    hist = [("13:40:12", None, "Created", "API", "Misia vytvorená cez web UI"),
            ("13:40:13", "Created", "Submitted", "API", "submitMission OK (KUKA 200)"),
            ("13:40:31", "Submitted", "InProgress", "Sync", "Robot KMP-01 prevzal misiu")]
    for i, (t, prev, st, src, msg) in enumerate(hist):
        y = cy + 76 + i * 92
        if i < len(hist) - 1:
            s.line(rx + 28, y + 8, rx + 28, y + 92 - 8, stroke="#C9D1DB", sw=2)
        s.add(f'<circle cx="{rx + 28}" cy="{y - 4}" r="7" fill="{BADGE[st][1]}"/>')
        s.text(rx + 48, y + 1, t, size=12.5, fill=MUTED, weight="600")
        bx = rx + 116
        if prev:
            w = s.badge(bx, y, prev, size=11)
            s.text(bx + w + 10, y + 1, "→", size=13, fill=MUTED, anchor="middle")
            bx += w + 20
        s.badge(bx, y, st, size=11)
        s.rect(rx + rw - 66, y - 14, 50, 20, fill="#F3F5F8", stroke=BORDER, rx=3)
        s.text(rx + rw - 41, y + 1, src, size=11, fill=MUTED, anchor="middle", weight="600")
        s.text(rx + 48, y + 26, msg, size=12.5, fill=TEXT)
    y = cy + 76 + 3 * 92
    s.add(f'<circle cx="{rx + 28}" cy="{y - 4}" r="7" fill="#FFFFFF" stroke="{BORDER}" stroke-width="2" stroke-dasharray="3 2"/>')
    s.text(rx + 48, y + 1, "čaká sa na ďalšiu zmenu (polling 5 s)…", size=12.5, fill=MUTED, italic=True)

    s.note(CX, H - 58, ["GET /api/missions/{id} + GET /api/missions/{id}/history. Pri aktívnej misii sa stránka obnovuje",
                        "pollingom; „Zrušiť misiu“ len pre Submitted/InProgress. Source: API = akcia klienta, Sync = polling."])

    if with_dialog:
        s.rect(0, 0, W, H, fill="#0B1620", stroke="none", rx=0, extra='fill-opacity="0.45"')
        dw, dh = 460, 236
        dx, dy = (W - dw) / 2, (H - dh) / 2 - 20
        s.rect(dx, dy, dw, dh, rx=8, stroke="none", extra='filter="url(#shadow)"')
        s.parts.insert(0, '<defs><filter id="shadow" x="-10%" y="-10%" width="120%" height="130%">'
                          '<feDropShadow dx="0" dy="6" stdDeviation="10" flood-opacity="0.25"/></filter></defs>')
        s.circle(dx + 36, dy + 40, 16, "#FDECEC")
        s.text(dx + 36, dy + 46, "!", size=16, fill="#B42318", weight="800", anchor="middle")
        s.text(dx + 64, dy + 46, "Zrušiť misiu M-2026-0142?", size=17, weight="700")
        s.text(dx + 24, dy + 88, "Middleware zavolá cancelMission na KUKA FM. Misia prejde", size=13, fill=TEXT)
        s.text(dx + 24, dy + 108, "do stavu Cancelling; finálny stav Cancelled potvrdí", size=13, fill=TEXT)
        s.text(dx + 24, dy + 128, "až nasledujúca synchronizácia.", size=13, fill=TEXT)
        s.text(dx + 24, dy + 156, "Akcia sa zapíše do audit logu.", size=12, fill=MUTED, italic=True)
        s.button(dx + dw - 24 - 150, dy + dh - 56, 150, "Áno, zrušiť misiu", "danger-solid")
        s.button(dx + dw - 24 - 150 - 12 - 90, dy + dh - 56, 90, "Späť", "secondary")
    s.save(out)


# =========================================================================
# 6.5 Roboty
# =========================================================================
ROBOTS = [
    ("KMP-01", "KUKA-AMR-1001", "Zaneprázdnený", 78, "C-03-WAIT", "M-2026-0142", "pred 2 s"),
    ("KMP-02", "KUKA-AMR-1002", "Zaneprázdnený", 54, "D-11", "M-2026-0140", "pred 2 s"),
    ("KMP-03", "KUKA-AMR-1003", "Dostupný", 91, "CHARGE-01", None, "pred 2 s"),
    ("KMP-04", "KUKA-AMR-1004", "Dostupný", 66, "A-02", None, "pred 2 s"),
    ("KMP-05", "KUKA-AMR-1005", "Dostupný", 23, "B-01", None, "pred 2 s"),
    ("KMP-06", "KUKA-AMR-1006", "Offline", 12, "B-07-DROP", None, "pred 14 min"),
]


def robots(out):
    s = Svg()
    s.chrome("roboty", "Roboty")
    y0 = TOP + 62
    s.text(CX, y0, "Roboty", size=24, weight="700")
    s.rect(CX + CW - 150, y0 - 20, 150, 26, fill="#F0F7F2", stroke="#B7DFC4", rx=13)
    s.circle(CX + CW - 136, y0 - 7, 4, "#22A559")
    s.text(CX + CW - 126, y0 - 2, "Live · obnova 5 s", size=12, fill="#1E6B3A")

    # súhrnné dlaždice
    ty = y0 + 20
    tiles = [("Spolu", "6", TEXT, None), ("Dostupné", "3", "#1E6B3A", "Dostupný"),
             ("Zaneprázdnené", "2", PRIMARY, "Zaneprázdnený"), ("Offline", "1", "#4A5563", "Offline")]
    tw = (CW - 3 * 16) / 4
    for i, (label, n, col, key) in enumerate(tiles):
        x = CX + i * (tw + 16)
        s.rect(x, ty, tw, 72, rx=6, fill=PANEL if i == 0 else BG)
        if key:
            s.rect(x, ty, 4, 72, fill=BADGE[key][1], stroke="none", rx=2)
        s.text(x + 18, ty + 28, label, size=12.5, fill=MUTED, weight="600")
        s.text(x + 18, ty + 58, n, size=26, fill=col, weight="700")

    # filtre + prepínač zobrazenia
    fy = ty + 92
    s.rect(CX, fy, 260, 34)
    s.text(CX + 12, fy + 22, "⌕  Hľadať robota…", size=13, fill=MUTED, italic=True)
    s.input(CX + 272, fy, 170, "", "Stav: všetky", select=True)
    s.rect(CX + CW - 156, fy, 156, 34, rx=5)
    s.rect(CX + CW - 155, fy + 1, 77, 32, fill="#DCE6F1", stroke="none", rx=4)
    s.text(CX + CW - 117, fy + 22, "▦ Karty", size=12.5, fill=PRIMARY, weight="700", anchor="middle")
    s.text(CX + CW - 39, fy + 22, "☰ Tabuľka", size=12.5, fill=MUTED, anchor="middle")

    # karty
    gy = fy + 50
    cw = (CW - 2 * 16) / 3
    ch = 176
    for i, (name, kid, st, bat, node, mission, seen) in enumerate(ROBOTS):
        x = CX + (i % 3) * (cw + 16)
        y = gy + (i // 3) * (ch + 16)
        off = st == "Offline"
        s.rect(x, y, cw, ch, rx=6, fill="#FAFBFC" if off else BG)
        tc = MUTED if off else TEXT
        s.text(x + 16, y + 30, name, size=17, fill=tc, weight="700")
        s.text(x + 16, y + 48, kid, size=11.5, fill=MUTED)
        bw = int(len(st) * 12 * 0.6) + 18
        s.badge(x + cw - 16 - bw, y + 28, st)

        # batéria
        s.text(x + 16, y + 80, "Batéria", size=11.5, fill=MUTED, weight="600")
        bcol = "#B42318" if bat < 25 else ("#D69E00" if bat < 50 else "#22A559")
        if off:
            bcol = "#A0AAB6"
        s.rect(x + 80, y + 70, cw - 150, 12, fill="#ECEFF3", stroke="none", rx=6)
        s.rect(x + 80, y + 70, (cw - 150) * bat / 100, 12, fill=bcol, stroke="none", rx=6)
        s.text(x + cw - 16, y + 81, f"{bat} %", size=12.5, fill=tc, weight="700", anchor="end")

        s.text(x + 16, y + 110, "Uzol", size=11.5, fill=MUTED, weight="600")
        s.text(x + 80, y + 110, node, size=13, fill=tc, weight="600")
        s.text(x + 16, y + 134, "Misia", size=11.5, fill=MUTED, weight="600")
        if mission:
            s.text(x + 80, y + 134, mission + "  ›", size=13, fill=PRIMARY, weight="600")
        else:
            s.text(x + 80, y + 134, "—", size=13, fill=MUTED)

        s.line(x, y + 148, x + cw, y + 148, stroke="#E4E8EE")
        s.text(x + 16, y + 167, ("⚠ naposledy videný " if off else "aktualizované ") + seen, size=11.5,
               fill="#8A5A00" if off else MUTED, italic=off)

    s.note(CX + CW - 500, H - 40, ["GET /api/robots z DB (UC6). Stav plní UC7 – polling KUKA FM."], w=500)
    s.save(out)


if __name__ == "__main__":
    here = Path(__file__).parent
    missions_list(here / "6.1-zoznam-misii.svg")
    mission_new(here / "6.2-nova-misia.svg")
    mission_detail(here / "6.3-detail-misie.svg")
    mission_detail(here / "6.4-zrusenie-misie.svg", with_dialog=True)
    robots(here / "6.5-roboty.svg")
    print("OK")
