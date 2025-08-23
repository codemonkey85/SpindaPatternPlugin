using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PKHeX.Core;

namespace SpindaPatternPlugin.GUI;

/// <summary>
/// Form for editing Spinda's unique spot patterns.
/// </summary>
public partial class SpindaPatternForm : Form
{
    private readonly PKM pokemon;
    
    /// <summary>
    /// Base Spinda sprite without spots.
    /// </summary>
    private Image? baseSprite;
    
    /// <summary>
    /// Shiny variant of the base sprite.
    /// </summary>
    private Image? shinySprite;
    
    /// <summary>
    /// Mask image used to clip spots to the head area only.
    /// </summary>
    private Image? headMask;
    
    /// <summary>
    /// Face overlay applied after spots for proper layering.
    /// </summary>
    private Image? faceOverlay;
    
    /// <summary>
    /// Mouth overlay to ensure spots don't cover facial features.
    /// </summary>
    private Image? mouthOverlay;
    
    /// <summary>
    /// The current pattern value determining spot positions.
    /// </summary>
    private uint patternValue;
    
    /// <summary>
    /// Whether to use PID (older games) or EC (newer games) for patterns.
    /// </summary>
    private readonly bool usePID;

    /// <summary>
    /// Creates the pattern editor form.
    /// </summary>
    /// <param name="pkm">The Pokémon to edit.</param>
    public SpindaPatternForm(PKM pkm)
    {
        pokemon = pkm;
        // Older games use PID, newer games use EC
        usePID = pokemon.Format <= 4;
        
        // BDSP stores the pattern value backwards
        // We need to flip it to show the right pattern
        if (!usePID && IsBDSP())
        {
            patternValue = SwapEndian(pokemon.EncryptionConstant);
        }
        else
        {
            patternValue = usePID ? pokemon.PID : pokemon.EncryptionConstant;
        }
        
        InitializeComponent();
        SetupFormAfterInit();
        LoadSprites();
        UpdatePreview();
    }
    
    /// <summary>
    /// Checks if the Pokémon is from Brilliant Diamond or Shining Pearl.
    /// </summary>
    /// <returns>True if from BDSP, false otherwise.</returns>
    private bool IsBDSP() => pokemon.Version is GameVersion.BD or GameVersion.SP;
    
    /// <summary>
    /// Reverses the byte order of a number.
    /// </summary>
    /// <param name="value">The number to reverse.</param>
    /// <returns>The reversed number.</returns>
    /// <remarks>
    /// BDSP stores pattern values backwards compared to other games.
    /// This fixes the pattern display.
    /// </remarks>
    private static uint SwapEndian(uint value)
    {
        return ((value & 0x000000FF) << 24) |
               ((value & 0x0000FF00) << 8) |
               ((value & 0x00FF0000) >> 8) |
               ((value & 0xFF000000) >> 24);
    }

    /// <summary>
    /// Sets up the form after it's created.
    /// </summary>
    private void SetupFormAfterInit()
    {
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        // Show the right label for this game
        patternLabel.Text = usePID ? "PID:" : "Encryption Constant:";
        shinyCheckbox.Checked = pokemon.IsShiny;
        patternInput.Text = patternValue.ToString("X8");
    }

    /// <summary>
    /// Loads Spinda sprite resources from embedded resources.
    /// </summary>
    private void LoadSprites()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            
            // Load each sprite from embedded resources
            // Resource names follow the pattern: SpindaPatternPlugin.Resources.img.spinda.filename.png
            string resourcePrefix = "SpindaPatternPlugin.Resources.img.spinda.";
            
            foreach (var resourceName in resourceNames)
            {
                if (!resourceName.StartsWith(resourcePrefix))
                    continue;
                    
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    continue;
                    
                var image = Image.FromStream(stream);
                
                if (resourceName.EndsWith("327-spotless.png"))
                    baseSprite = image;
                else if (resourceName.EndsWith("327-spotless-shiny.png"))
                    shinySprite = image;
                else if (resourceName.EndsWith("327-head.png"))
                    headMask = image;
                else if (resourceName.EndsWith("327-face.png"))
                    faceOverlay = image;
                else if (resourceName.EndsWith("327-mouth.png"))
                    mouthOverlay = image;
                else
                    image.Dispose(); // Dispose if we don't use it
            }
            
            // Debug output if no sprites were loaded
            if (baseSprite == null && shinySprite == null)
            {
                System.Diagnostics.Debug.WriteLine("No Spinda sprites were loaded from embedded resources.");
                System.Diagnostics.Debug.WriteLine("Available resources:");
                foreach (var name in resourceNames)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {name}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading Spinda sprites: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the preview image with the current pattern and shiny status.
    /// </summary>
    private void UpdatePreview()
    {
        bool isShiny = shinyCheckbox.Checked || pokemon.IsShiny;
        Image? sprite = isShiny && shinySprite != null ? shinySprite : baseSprite;
        
        // Draw a simple Spinda if sprite files are missing
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
                
                // Make sure spots only show on the head, not the body
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
            
            // Draw face features on top of spots
            // This makes sure eyes and mouth show properly
            if (faceOverlay != null)
                g.DrawImage(faceOverlay, 0, 0);
            
            if (mouthOverlay != null)
                g.DrawImage(mouthOverlay, 0, 0);
        }
        
        pictureBox.Image?.Dispose();
        pictureBox.Image = result;
    }

    /// <summary>
    /// Clips spots to only show on Spinda's head.
    /// </summary>
    /// <param name="source">The spots to clip.</param>
    /// <param name="mask">The head shape.</param>
    /// <returns>Spots that only appear on the head.</returns>
    private static Bitmap MaskSpots(Bitmap source, Image mask)
    {
        Bitmap result = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        Bitmap maskBitmap = new(mask.Width, mask.Height, PixelFormat.Format32bppArgb);
        
        // Draw mask into 32bpp format for consistent processing
        using (Graphics g = Graphics.FromImage(maskBitmap))
        {
            g.DrawImage(mask, 0, 0);
        }
        
        // Use fast pixel access for better performance
        BitmapData sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData maskData = maskBitmap.LockBits(new Rectangle(0, 0, maskBitmap.Width, maskBitmap.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        
        try
        {
            int sourceStride = sourceData.Stride;
            int maskStride = maskData.Stride;
            int resultStride = resultData.Stride;
            int width = source.Width;
            int height = source.Height;
            
            // Create byte arrays for pixel data
            byte[] sourceBytes = new byte[Math.Abs(sourceStride) * height];
            byte[] maskBytes = new byte[Math.Abs(maskStride) * height];
            byte[] resultBytes = new byte[Math.Abs(resultStride) * height];
            
            // Copy pixel data from bitmaps to arrays
            Marshal.Copy(sourceData.Scan0, sourceBytes, 0, sourceBytes.Length);
            Marshal.Copy(maskData.Scan0, maskBytes, 0, maskBytes.Length);
            
            // Check each pixel to see if it should show
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * sourceStride;
                
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = rowOffset + (x * 4);
                    
                    byte sourceAlpha = sourceBytes[pixelOffset + 3];
                    byte maskAlpha = maskBytes[pixelOffset + 3];
                    
                    // Only show the spot where the mask allows it
                    if (sourceAlpha > 0 && maskAlpha > 0)
                    {
                        byte alpha = (byte)((sourceAlpha * maskAlpha) / 255);
                        resultBytes[pixelOffset] = sourceBytes[pixelOffset];         // Blue
                        resultBytes[pixelOffset + 1] = sourceBytes[pixelOffset + 1]; // Green
                        resultBytes[pixelOffset + 2] = sourceBytes[pixelOffset + 2]; // Red
                        resultBytes[pixelOffset + 3] = alpha;                        // Alpha
                    }
                    // Otherwise, leave transparent
                }
            }
            
            // Copy processed data back to result bitmap
            Marshal.Copy(resultBytes, 0, resultData.Scan0, resultBytes.Length);
        }
        finally
        {
            source.UnlockBits(sourceData);
            maskBitmap.UnlockBits(maskData);
            result.UnlockBits(resultData);
        }
        
        maskBitmap.Dispose();
        return result;
    }

    /// <summary>
    /// Renders Spinda's four spots based on the pattern value.
    /// </summary>
    /// <param name="g">Graphics context to draw on.</param>
    /// <param name="value">The 32-bit pattern value.</param>
    private void RenderSpots(Graphics g, uint value)
    {
        // Each digit of the pattern controls where a spot appears
        // The 8-digit hex number is split into pairs for each spot
        string hex = value.ToString("X8");
        
        // Each pair of digits controls one spot:
        // Digits 0-1: Right face spot
        // Digits 2-3: Left face spot
        // Digits 4-5: Right ear spot
        // Digits 6-7: Left ear spot
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

        // Draw each spot in its area on Spinda's head
        // Each spot can move around within its own box
        // The numbers match how the game draws spots
        PlaceSpot(g, rightFaceY, rightFaceX, 0.40f, 0.43f, 0.40f, 0.40f, 0.39f, 0.41f, imageSize, isShiny, 6, 6);
        PlaceSpot(g, leftFaceY, leftFaceX, 0.20f, 0.39f, 0.39f, 0.39f, 0.35f, 0.39f, imageSize, isShiny, -6, 0);
        PlaceSpot(g, rightEarY, rightEarX, 0.57f, 0.24f, 0.39f, 0.39f, 0.38f, 0.41f, imageSize, isShiny, 6, 30);
        PlaceSpot(g, leftEarY, leftEarX, 0.17f, 0.12f, 0.40f, 0.40f, 0.33f, 0.36f, imageSize, isShiny, -6, 0);
    }
    
    /// <summary>
    /// Draws one spot on Spinda.
    /// </summary>
    /// <param name="g">Graphics to draw on.</param>
    /// <param name="yDigit">Vertical position (0-15).</param>
    /// <param name="xDigit">Horizontal position (0-15).</param>
    /// <param name="containerLeft">Left edge of spot area.</param>
    /// <param name="containerTop">Top edge of spot area.</param>
    /// <param name="containerWidth">Width of spot area.</param>
    /// <param name="containerHeight">Height of spot area.</param>
    /// <param name="spotWidthPercent">Spot width.</param>
    /// <param name="spotHeightPercent">Spot height.</param>
    /// <param name="imageSize">Image size.</param>
    /// <param name="isShiny">Use shiny colors.</param>
    /// <param name="rotation">Spot tilt.</param>
    /// <param name="containerRotation">Area tilt.</param>
    private static void PlaceSpot(Graphics g, int yDigit, int xDigit, float containerLeft, float containerTop, 
        float containerWidth, float containerHeight, float spotWidthPercent, float spotHeightPercent, 
        int imageSize, bool isShiny, float rotation, float containerRotation = 0)
    {
        // Convert 0-15 to position in the spot's area
        // Spots can move around 66% of their area
        float topPercent = (yDigit / 15.0f) * 66.0f;
        float leftPercent = (xDigit / 15.0f) * 66.0f;
        
        float containerX = containerLeft * imageSize;
        float containerY = containerTop * imageSize;
        float containerPixelWidth = containerWidth * imageSize;
        float containerPixelHeight = containerHeight * imageSize;
        
        float spotXInContainer = (leftPercent / 100.0f) * containerPixelWidth;
        float spotYInContainer = (topPercent / 100.0f) * containerPixelHeight;
        
        // Rotate the spot area if needed (for ear spots on angled parts)
        // This makes spots follow the shape of Spinda's head
        float spotX, spotY;
        if (Math.Abs(containerRotation) > 0.1f)
        {
            float containerCenterX = containerPixelWidth / 2;
            float containerCenterY = containerPixelHeight / 2;
            
            // Calculate position from center
            float relX = spotXInContainer - containerCenterX;
            float relY = spotYInContainer - containerCenterY;
            
            // Rotate the position
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
        
        // Use the right colors for spots
        // Shiny = green, Normal = red
        Color spotColor = isShiny ? 
            Color.FromArgb(255, 183, 199, 92) :  // Green
            Color.FromArgb(255, 255, 59, 79);    // Red
        Color borderColor = isShiny ?
            Color.FromArgb(255, 150, 170, 70) :  // Dark green
            Color.FromArgb(255, 220, 40, 60);    // Dark red
        
        var originalTransform = g.Transform;
        
        float drawX = spotX - spotWidth/2;
        float drawY = spotY - spotHeight/2;
        
        // Tilt the spot slightly for a natural look
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

    /// <summary>
    /// Makes a random pattern.
    /// </summary>
    private void OnRandomize(object? sender, EventArgs e)
    {
        // Use good randomness for better patterns
        uint newValue;
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            byte[] bytes = new byte[4];
            rng.GetBytes(bytes);
            newValue = BitConverter.ToUInt32(bytes, 0);
        }
        
        // For older games, make sure shiny patterns stay shiny
        // The PID controls both pattern and shininess
        if (shinyCheckbox.Checked && usePID)
        {
            uint tid = pokemon.TID16;
            uint sid = pokemon.SID16;
            uint psv = (tid ^ sid) >> 3;
            // Keep the pattern part, fix the shiny part
            newValue = (newValue & 0xFFFF0000) | (psv << 3) | (uint)(newValue & 0x7);
        }
        
        patternValue = newValue;
        patternInput.Text = newValue.ToString("X8");
        UpdatePreview();
    }

    /// <summary>
    /// Saves the pattern and closes the window.
    /// </summary>
    private void OnApply(object? sender, EventArgs e)
    {
        // Make sure it's a Spinda
        if (pokemon.Species != (ushort)Species.Spinda)
        {
            pokemon.Species = (ushort)Species.Spinda;
            pokemon.Form = 0;
        }
        
        // For older games, PID controls pattern and shininess together
        // Update both carefully
        if (usePID)
        {
            pokemon.PID = patternValue;
            
            // Make shiny or not shiny as needed
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
            // For newer games, EC controls pattern and PID controls shininess
            // BDSP needs the bytes reversed
            if (IsBDSP())
            {
                pokemon.EncryptionConstant = SwapEndian(patternValue);
            }
            else
            {
                pokemon.EncryptionConstant = patternValue;
            }
            
            // Make sure PID exists
            if (pokemon.PID == 0)
            {
                using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                pokemon.PID = BitConverter.ToUInt32(bytes, 0);
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
        
        // Update ability based on game rules
        // Older games: PID decides ability
        // Newer games: Ability is separate
        if (usePID)
        {
            int abilityIndex = (int)(pokemon.PID & 1);
            pokemon.RefreshAbility(abilityIndex);
        }
        else
        {
            // Make sure ability is valid
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

}
