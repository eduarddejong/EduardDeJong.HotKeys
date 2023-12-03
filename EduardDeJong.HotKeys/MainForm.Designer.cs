namespace EduardDeJong.HotKeys;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        HeaderLabel = new System.Windows.Forms.Label();
        NotifyIcon = new System.Windows.Forms.NotifyIcon(components);
        SuspendLayout();
        // 
        // HeaderLabel
        // 
        HeaderLabel.AutoSize = true;
        HeaderLabel.Font = new System.Drawing.Font("Segoe UI", 24F);
        HeaderLabel.Location = new System.Drawing.Point(12, 9);
        HeaderLabel.Name = "HeaderLabel";
        HeaderLabel.Size = new System.Drawing.Size(340, 45);
        HeaderLabel.TabIndex = 0;
        HeaderLabel.Text = "(HotKeys app window)";
        // 
        // NotifyIcon
        // 
        NotifyIcon.Visible = true;
        NotifyIcon.Click += NotifyIcon_Click;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(384, 61);
        Controls.Add(HeaderLabel);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "MainForm";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "Eduard de Jong HotKeys";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label HeaderLabel;
    private System.Windows.Forms.NotifyIcon NotifyIcon;
}
