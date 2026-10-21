namespace LetItBee.HoneyPilot
{
    // Input intent survives server latency. Holding grip means carrying;
    // releasing means letting go, even if the pickup ACK has not arrived yet.
    public sealed class BeeHoneyGrip
    {
        public bool Pressed { get; private set; }
        public bool ReleasePending { get; private set; }
        bool attempted, picking, owns;

        public bool Sample(float value)
        {
            bool next = Pressed ? value > 0.45f : value >= 0.65f;
            bool released = Pressed && !next;
            if (!Pressed && next) ReleasePending = false;
            if (released) { ReleasePending = owns || picking; attempted = false; }
            Pressed = next;
            return released;
        }

        public string Choose(bool live, bool busy, string state, bool canPick, bool canDeposit)
        {
            if (!live || busy) return null;
            if (state == "held") {
                if (Pressed) owns = true;
                if (ReleasePending) { ReleasePending = false; owns = false; return canDeposit ? "deposit" : "drop"; }
            } else {
                if (!picking) { owns = false; ReleasePending = false; }
                if (Pressed && !attempted && canPick && (state == "ready" || state == "loose")) {
                    attempted = picking = true;
                    return "pick";
                }
            }
            return null;
        }

        public void Complete(string kind, bool accepted)
        {
            if (kind != "pick") return;
            picking = false;
            if (accepted) { owns = Pressed; ReleasePending = !Pressed; }
            else ReleasePending = false;
        }

        public void Reset() { Pressed = ReleasePending = attempted = picking = owns = false; }
    }
}
