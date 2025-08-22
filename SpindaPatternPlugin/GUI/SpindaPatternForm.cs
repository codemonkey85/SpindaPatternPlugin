using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;

namespace SpindaPatternPlugin.GUI;

/// <summary>
/// Form for editing Spinda's unique spot patterns.
/// </summary>
public partial class SpindaPatternForm : Form
{
    private readonly PKM pokemon;
    private PictureBox pictureBox = null!;
    private TextBox patternInput = null!;
    private Button randomizeButton = null!;
    private Button applyButton = null!;
    private Button cancelButton = null!;
    private CheckBox shinyCheckbox = null!;
    private Label patternLabel = null!;
    
    private Image? baseSprite;
    private Image? shinySprite;
    private Image? headMask;
    private Image? faceOverlay;
    private Image? mouthOverlay;
    
    private uint patternValue;
    private readonly bool usePID;

    public SpindaPatternForm(PKM pkm)
    {
        pokemon = pkm;
        usePID = pokemon.Format <= 4;
        
        if (!usePID && IsBDSP())
        {
            patternValue = SwapEndian(pokemon.EncryptionConstant);
        }
        else
        {
            patternValue = usePID ? pokemon.PID : pokemon.EncryptionConstant;
        }
        
        InitializeComponent();
        LoadSprites();
        UpdatePreview();
    }
    
    private bool IsBDSP() => pokemon.Version is GameVersion.BD or GameVersion.SP;
    
    private static uint SwapEndian(uint value)
    {
        return ((value & 0x000000FF) << 24) |
               ((value & 0x0000FF00) << 8) |
               ((value & 0x00FF0000) >> 8) |
               ((value & 0xFF000000) >> 24);
    }

    private void InitializeComponent()
    {
        Text = "Spinda Pattern Editor";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(400, 500);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        pictureBox = new PictureBox
        {
            Location = new Point(50, 20),
            Size = new Size(300, 300),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        Controls.Add(pictureBox);

        patternLabel = new Label
        {
            Text = usePID ? "PID:" : "Encryption Constant:",
            Location = new Point(50, 340),
            Size = new Size(120, 23),
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(patternLabel);

        patternInput = new TextBox
        {
            Location = new Point(175, 340),
            Size = new Size(100, 23),
            Font = new Font("Courier New", 9),
            MaxLength = 8,
            CharacterCasing = CharacterCasing.Upper
        };
        patternInput.TextChanged += OnPatternChanged;
        Controls.Add(patternInput);

        shinyCheckbox = new CheckBox
        {
            Text = "Shiny",
            Location = new Point(285, 340),
            Size = new Size(65, 23),
            Checked = pokemon.IsShiny
        };
        shinyCheckbox.CheckedChanged += OnShinyToggled;
        Controls.Add(shinyCheckbox);

        randomizeButton = new Button
        {
            Text = "Randomize",
            Location = new Point(50, 380),
            Size = new Size(100, 30)
        };
        randomizeButton.Click += OnRandomize;
        Controls.Add(randomizeButton);

        applyButton = new Button
        {
            Text = "Apply",
            Location = new Point(170, 430),
            Size = new Size(75, 30),
            DialogResult = DialogResult.OK
        };
        applyButton.Click += OnApply;
        Controls.Add(applyButton);

        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(255, 430),
            Size = new Size(75, 30),
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(cancelButton);

        patternInput.Text = patternValue.ToString("X8");
    }

    private void LoadSprites()
    {
        try
        {
            string[] searchPaths = 
            [
                System.IO.Path.Combine(Application.StartupPath, "Resources", "img", "spinda"),
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath) ?? "", "Resources", "img", "spinda"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "img", "spinda"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "SpindaPatternPlugin", "Resources", "img", "spinda"),
                @"C:\Users\Brand\source\repos\SpindaPatternPlugin\SpindaPatternPlugin\Resources\img\spinda",
            ];
            
            string? resourcePath = null;
            foreach (var path in searchPaths)
            {
                if (System.IO.Directory.Exists(path))
                {
                    resourcePath = path;
                    break;
                }
            }
            
            if (resourcePath == null)
                return;
            
            string spotlessPath = System.IO.Path.Combine(resourcePath, "327-spotless.png");
            string shinyPath = System.IO.Path.Combine(resourcePath, "327-spotless-shiny.png");
            string headPath = System.IO.Path.Combine(resourcePath, "327-head.png");
            string facePath = System.IO.Path.Combine(resourcePath, "327-face.png");
            string mouthPath = System.IO.Path.Combine(resourcePath, "327-mouth.png");
            
            if (System.IO.File.Exists(spotlessPath))
                baseSprite = Image.FromFile(spotlessPath);
            
            if (System.IO.File.Exists(shinyPath))
                shinySprite = Image.FromFile(shinyPath);
            
            if (System.IO.File.Exists(headPath))
                headMask = Image.FromFile(headPath);
            
            if (System.IO.File.Exists(facePath))
                faceOverlay = Image.FromFile(facePath);
            
            if (System.IO.File.Exists(mouthPath))
                mouthOverlay = Image.FromFile(mouthPath);
        }
        catch
        {
        }
    }

    private void UpdatePreview()
    {
        bool isShiny = shinyCheckbox.Checked || pokemon.IsShiny;
        Image? sprite = isShiny && shinySprite != null ? shinySprite : baseSprite;
        
        if (sprite == null)
        {
            DrawFallbackSpinda();
            return;
        }

        Bitmap result = new(sprite.Width, sprite.Height);
        using (Graphics g = Graphics.FromImage(result))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            
            g.DrawImage(sprite, 0, 0);
            
            using (Bitmap spotLayer = new(sprite.Width, sprite.Height))
            using (Graphics spotGraphics = Graphics.FromImage(spotLayer))
            {
                spotGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                RenderSpots(spotGraphics, patternValue);
                
                if (headMask != null)
                {
                    using Bitmap maskedSpots = MaskSpots(spotLayer, headMask);
                    g.DrawImage(maskedSpots, 0, 0);
                }
                else
                {
                    g.DrawImage(spotLayer, 0, 0);
                }
            }
            
            if (faceOverlay != null)
                g.DrawImage(faceOverlay, 0, 0);
            
            if (mouthOverlay != null)
                g.DrawImage(mouthOverlay, 0, 0);
        }
        
        pictureBox.Image?.Dispose();
        pictureBox.Image = result;
    }

    private static Bitmap MaskSpots(Bitmap source, Image mask)
    {
        Bitmap result = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        Bitmap maskBitmap = new(mask);
        
        using (Graphics g = Graphics.FromImage(result))
        {
            g.Clear(Color.Transparent);
            
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color maskPixel = maskBitmap.GetPixel(x, y);
                    Color sourcePixel = source.GetPixel(x, y);
                    
                    if (maskPixel.A > 0 && sourcePixel.A > 0)
                    {
                        int alpha = (sourcePixel.A * maskPixel.A) / 255;
                        Color blendedColor = Color.FromArgb(alpha, sourcePixel.R, sourcePixel.G, sourcePixel.B);
                        result.SetPixel(x, y, blendedColor);
                    }
                }
            }
        }
        
        maskBitmap.Dispose();
        return result;
    }

    private void RenderSpots(Graphics g, uint value)
    {
        string hex = value.ToString("X8");
        
        int rightFaceY = Convert.ToInt32(hex[..1], 16);
        int rightFaceX = Convert.ToInt32(hex.Substring(1, 1), 16);
        
        int leftFaceY = Convert.ToInt32(hex.Substring(2, 1), 16);
        int leftFaceX = Convert.ToInt32(hex.Substring(3, 1), 16);
        
        int rightEarY = Convert.ToInt32(hex.Substring(4, 1), 16);
        int rightEarX = Convert.ToInt32(hex.Substring(5, 1), 16);
        
        int leftEarY = Convert.ToInt32(hex.Substring(6, 1), 16);
        int leftEarX = Convert.ToInt32(hex.Substring(7, 1), 16);

        int imageSize = baseSprite?.Width ?? shinySprite?.Width ?? 300;
        bool isShiny = shinyCheckbox.Checked || pokemon.IsShiny;

        PlaceSpot(g, rightFaceY, rightFaceX, 0.40f, 0.43f, 0.40f, 0.40f, 0.39f, 0.41f, imageSize, isShiny, 6, 6);
        PlaceSpot(g, leftFaceY, leftFaceX, 0.20f, 0.39f, 0.39f, 0.39f, 0.35f, 0.39f, imageSize, isShiny, -6, 0);
        PlaceSpot(g, rightEarY, rightEarX, 0.57f, 0.24f, 0.39f, 0.39f, 0.38f, 0.41f, imageSize, isShiny, 6, 30);
        PlaceSpot(g, leftEarY, leftEarX, 0.17f, 0.12f, 0.40f, 0.40f, 0.33f, 0.36f, imageSize, isShiny, -6, 0);
    }
    
    private static void PlaceSpot(Graphics g, int yDigit, int xDigit, float containerLeft, float containerTop, 
        float containerWidth, float containerHeight, float spotWidthPercent, float spotHeightPercent, 
        int imageSize, bool isShiny, float rotation, float containerRotation = 0)
    {
        float topPercent = (yDigit / 15.0f) * 66.0f;
        float leftPercent = (xDigit / 15.0f) * 66.0f;
        
        float containerX = containerLeft * imageSize;
        float containerY = containerTop * imageSize;
        float containerPixelWidth = containerWidth * imageSize;
        float containerPixelHeight = containerHeight * imageSize;
        
        float spotXInContainer = (leftPercent / 100.0f) * containerPixelWidth;
        float spotYInContainer = (topPercent / 100.0f) * containerPixelHeight;
        
        float spotX, spotY;
        if (Math.Abs(containerRotation) > 0.1f)
        {
            float containerCenterX = containerPixelWidth / 2;
            float containerCenterY = containerPixelHeight / 2;
            
            float relX = spotXInContainer - containerCenterX;
            float relY = spotYInContainer - containerCenterY;
            
            double containerRad = containerRotation * Math.PI / 180.0;
            float rotatedX = (float)(relX * Math.Cos(containerRad) - relY * Math.Sin(containerRad));
            float rotatedY = (float)(relX * Math.Sin(containerRad) + relY * Math.Cos(containerRad));
            
            spotX = containerX + containerCenterX + rotatedX;
            spotY = containerY + containerCenterY + rotatedY;
        }
        else
        {
            spotX = containerX + spotXInContainer;
            spotY = containerY + spotYInContainer;
        }
        
        float spotWidth = containerPixelWidth * spotWidthPercent;
        float spotHeight = containerPixelHeight * spotHeightPercent;
        
        Color spotColor = isShiny ? 
            Color.FromArgb(255, 183, 199, 92) :
            Color.FromArgb(255, 255, 59, 79);
        Color borderColor = isShiny ?
            Color.FromArgb(255, 150, 170, 70) :
            Color.FromArgb(255, 220, 40, 60);
        
        var originalTransform = g.Transform;
        
        float drawX = spotX - spotWidth/2;
        float drawY = spotY - spotHeight/2;
        
        if (Math.Abs(rotation) > 0.1f)
        {
            g.TranslateTransform(spotX, spotY);
            g.RotateTransform(rotation);
            g.TranslateTransform(-spotX, -spotY);
        }
        
        using (Brush spotBrush = new SolidBrush(spotColor))
        using (Pen borderPen = new(borderColor, 1))
        {
            g.FillEllipse(spotBrush, drawX, drawY, spotWidth, spotHeight);
            g.DrawEllipse(borderPen, drawX, drawY, spotWidth, spotHeight);
        }
        
        g.Transform = originalTransform;
    }

    private void DrawFallbackSpinda()
    {
        Bitmap bmp = new(300, 300);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            using (Brush bodyBrush = new SolidBrush(Color.FromArgb(255, 245, 235, 215)))
            {
                g.FillEllipse(bodyBrush, 100, 100, 100, 100);
                g.FillEllipse(bodyBrush, 80, 60, 60, 60);
                g.FillEllipse(bodyBrush, 160, 60, 60, 60);
            }
            
            RenderBasicSpots(g, patternValue);
        }
        
        pictureBox.Image?.Dispose();
        pictureBox.Image = bmp;
    }

    private void RenderBasicSpots(Graphics g, uint value)
    {
        byte spot1 = (byte)(value & 0xFF);
        byte spot2 = (byte)((value >> 8) & 0xFF);
        byte spot3 = (byte)((value >> 16) & 0xFF);
        byte spot4 = (byte)((value >> 24) & 0xFF);

        bool isShiny = shinyCheckbox.Checked || pokemon.IsShiny;
        Color spotColor = isShiny ? Color.FromArgb(255, 183, 199, 92) : Color.FromArgb(255, 255, 59, 79);

        using Brush spotBrush = new SolidBrush(spotColor);
        DrawBasicSpot(g, spotBrush, spot1, 120, 140);
        DrawBasicSpot(g, spotBrush, spot2, 180, 140);
        DrawBasicSpot(g, spotBrush, spot3, 100, 80);
        DrawBasicSpot(g, spotBrush, spot4, 200, 80);
    }

    private static void DrawBasicSpot(Graphics g, Brush brush, byte spotByte, int baseX, int baseY)
    {
        int xOffset = (spotByte & 0x0F) - 8;
        int yOffset = ((spotByte >> 4) & 0x0F) - 8;
        
        g.FillEllipse(brush, baseX + xOffset, baseY + yOffset, 20, 20);
    }

    private void OnPatternChanged(object? sender, EventArgs e)
    {
        if (uint.TryParse(patternInput.Text, System.Globalization.NumberStyles.HexNumber, null, out uint value))
        {
            patternValue = value;
            UpdatePreview();
        }
    }

    private void OnShinyToggled(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void OnRandomize(object? sender, EventArgs e)
    {
        Random rand = new();
        uint newValue = (uint)rand.Next();
        
        if (shinyCheckbox.Checked && usePID)
        {
            uint tid = pokemon.TID16;
            uint sid = pokemon.SID16;
            uint psv = (tid ^ sid) >> 3;
            newValue = (newValue & 0xFFFF0000) | (psv << 3) | (uint)rand.Next(8);
        }
        
        patternValue = newValue;
        patternInput.Text = newValue.ToString("X8");
        UpdatePreview();
    }

    private void OnApply(object? sender, EventArgs e)
    {
        if (pokemon.Species != (ushort)Species.Spinda)
        {
            pokemon.Species = (ushort)Species.Spinda;
            pokemon.Form = 0;
        }
        
        if (usePID)
        {
            pokemon.PID = patternValue;
            
            if (shinyCheckbox.Checked && !pokemon.IsShiny)
            {
                CommonEdits.SetShiny(pokemon);
                patternValue = pokemon.PID;
                patternInput.Text = patternValue.ToString("X8");
            }
            else if (!shinyCheckbox.Checked && pokemon.IsShiny)
            {
                pokemon.SetUnshiny();
                patternValue = pokemon.PID;
                patternInput.Text = patternValue.ToString("X8");
            }
        }
        else
        {
            if (IsBDSP())
            {
                pokemon.EncryptionConstant = SwapEndian(patternValue);
            }
            else
            {
                pokemon.EncryptionConstant = patternValue;
            }
            
            if (pokemon.PID == 0)
            {
                Random rand = new();
                pokemon.PID = (uint)rand.Next();
            }
            
            if (shinyCheckbox.Checked && !pokemon.IsShiny)
            {
                CommonEdits.SetShiny(pokemon);
            }
            else if (!shinyCheckbox.Checked && pokemon.IsShiny)
            {
                pokemon.SetUnshiny();
            }
        }
        
        pokemon.ClearNickname();
        
        if (usePID)
        {
            int abilityIndex = (int)(pokemon.PID & 1);
            pokemon.RefreshAbility(abilityIndex);
        }
        else
        {
            var pi = pokemon.PersonalInfo;
            if (pokemon.Ability == 0 || pi.GetIndexOfAbility(pokemon.Ability) < 0)
            {
                CommonEdits.SetAbilityIndex(pokemon, 0);
            }
        }
        
        pokemon.ResetPartyStats();
        pokemon.RefreshChecksum();
        
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            baseSprite?.Dispose();
            shinySprite?.Dispose();
            headMask?.Dispose();
            faceOverlay?.Dispose();
            mouthOverlay?.Dispose();
            pictureBox.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}