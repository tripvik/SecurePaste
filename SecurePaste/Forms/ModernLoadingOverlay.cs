// ModernLoadingOverlay.cs
using SecurePaste.Extensions;
using System.Drawing.Drawing2D;

public class ModernLoadingOverlay : Form
{
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private float _spinnerAngle = 0;
    private bool _fadeIn = true;

    public string MessageText { get; private set; } = "Processing...";

    public ModernLoadingOverlay()
    {
        // Form properties
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.ShowInTaskbar = false;
        this.TopMost = true;
        this.Size = new Size(320, 110);
        this.Opacity = 0; // Start fully transparent for fade-in
        this.BackColor = Color.White; // This color will be made transparent
        this.TransparencyKey = Color.White;

        // Animation timer for the spinner
        _animationTimer = new System.Windows.Forms.Timer { Interval = 20 };
        _animationTimer.Tick += (sender, args) =>
        {
            _spinnerAngle = (_spinnerAngle + 8) % 360;
            this.Invalidate(); // Redraw the form
        };

        // Timer for fade-in/out effect
        _fadeTimer = new System.Windows.Forms.Timer { Interval = 15 };
        _fadeTimer.Tick += FadeTimer_Tick;

        // Double buffer for smooth drawing
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    public void ShowOverlay(string message)
    {
        MessageText = message;

        // Position at bottom-right of the primary screen
        var screen = Screen.PrimaryScreen;
        if (screen != null)
        {
            int margin = 20;
            this.Location = new Point(
                screen.WorkingArea.Right - this.Width - margin,
                screen.WorkingArea.Bottom - this.Height - margin);
        }

        this.Opacity = 0;
        _fadeIn = true;
        this.Show();
        _animationTimer.Start();
        _fadeTimer.Start();
    }

    public void HideOverlay()
    {
        _fadeIn = false;
        _fadeTimer.Start();
    }

    public void UpdateMessage(string message)
    {
        MessageText = message;
        this.Invalidate();
    }

    private void FadeTimer_Tick(object? sender, EventArgs e)
    {
        if (_fadeIn)
        {
            this.Opacity += 0.08;
            if (this.Opacity >= 1.0)
            {
                _fadeTimer.Stop();
                this.Opacity = 1.0;
            }
        }
        else // Fading out
        {
            this.Opacity -= 0.08;
            if (this.Opacity <= 0)
            {
                _fadeTimer.Stop();
                this.Opacity = 0;
                _animationTimer.Stop();
                this.Hide();
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw the rounded background
        int cornerRadius = 20;
        using (var brush = new SolidBrush(Color.FromArgb(220, 30, 30, 35))) // Dark, semi-transparent background
        {
            e.Graphics.FillRoundedRectangle(brush, this.ClientRectangle, cornerRadius);
        }

        // Draw the animated spinner
        int spinnerSize = 40;
        var spinnerRect = new Rectangle(20, (this.Height - spinnerSize) / 2, spinnerSize, spinnerSize);
        using (var pen = new Pen(Color.FromArgb(0, 122, 204), 4)) // Modern blue
        {
            e.Graphics.DrawArc(pen, spinnerRect, _spinnerAngle, 270);
        }

        // Draw the message text
        var textRect = new Rectangle(spinnerRect.Right + 15, 0, this.Width - spinnerRect.Right - 30, this.Height);
        TextRenderer.DrawText(e.Graphics, MessageText, new Font("Segoe UI", 10F, FontStyle.Regular), textRect, Color.Gainsboro, TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer?.Dispose();
            _fadeTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}