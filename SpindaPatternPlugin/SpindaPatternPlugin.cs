using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;
using SpindaPatternPlugin.GUI;

namespace SpindaPatternPlugin;

/// <summary>
/// PKHeX plugin for customizing Spinda's unique spot patterns.
/// </summary>
public sealed class SpindaPatternPlugin : IPlugin
{
    /// <summary>
    /// Gets the plugin name shown in PKHeX's Tools menu.
    /// </summary>
    public string Name => "Spinda Pattern Editor";
    
    /// <summary>
    /// Gets the loading priority. Higher numbers load first.
    /// </summary>
    public int Priority => 1;

    /// <summary>
    /// Gets or sets the save file editor from PKHeX.
    /// </summary>
    public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
    
    /// <summary>
    /// Gets or sets the Pokémon editor from PKHeX.
    /// </summary>
    public IPKMView PKMEditor { get; private set; } = null!;
    
    /// <summary>
    /// The menu item added to PKHeX's Tools menu.
    /// </summary>
    private ToolStripMenuItem? menuItem;
    
    /// <summary>
    /// The Spots button shown when editing a Spinda.
    /// </summary>
    private Button? spotsButton;

    /// <summary>
    /// Sets up the plugin with PKHeX.
    /// </summary>
    /// <param name="args">Tools and editors from PKHeX.</param>
    public void Initialize(params object[] args)
    {
        // Get the tools we need from PKHeX
        SaveFileEditor = (ISaveFileProvider?)Array.Find(args, z => z is ISaveFileProvider) 
            ?? throw new ArgumentException("Save file editor not found");
        PKMEditor = (IPKMView?)Array.Find(args, z => z is IPKMView) 
            ?? throw new ArgumentException("Pokémon editor not found");
        var menu = (ToolStrip?)Array.Find(args, z => z is ToolStrip) 
            ?? throw new ArgumentException("Menu not found");
        SetupMenu(menu);
        
        CreateSpotsButton();
        
        // Watch for changes to show/hide the Spots button
        // The button only shows when editing a Spinda
        if (PKMEditor is Control control)
        {
            control.Enter += (_, _) => RefreshButtonVisibility();
            control.Leave += (_, _) => RefreshButtonVisibility();
            
            // Also watch when the user picks a different Pokémon
            var speciesControl = FindControl(control, "CB_Species");
            if (speciesControl is ComboBox speciesCombo)
            {
                speciesCombo.SelectedIndexChanged += (_, _) => RefreshButtonVisibility();
            }
        }
    }

    /// <summary>
    /// Sets up the plugin's menu item in PKHeX's Tools menu.
    /// </summary>
    /// <param name="menuStrip">The main menu strip from PKHeX.</param>
    private void SetupMenu(ToolStrip menuStrip)
    {
        var items = menuStrip.Items;
        var toolsItems = items.Find("Menu_Tools", false);
        if (toolsItems.Length == 0 || toolsItems[0] is not ToolStripDropDownItem tools)
            throw new ArgumentException("Menu_Tools not found in menu strip", nameof(menuStrip));
        
        menuItem = new ToolStripMenuItem(Name)
        {
            ShortcutKeys = Keys.Control | Keys.Shift | Keys.S
        };
        menuItem.Click += ShowPatternEditor;
        tools.DropDownItems.Add(menuItem);
        
        // Check if this game has Spinda
        // Hide the menu for games that don't have Spinda
        if (SaveFileEditor?.SAV != null)
        {
            bool spindaAvailable = SaveFileEditor.SAV switch
            {
                SAV3 => true,
                SAV4 => true,
                SAV5 => true,
                SAV6 => true,
                SAV7 => true,
                SAV7b => false,
                SAV8SWSH => true,
                SAV8LA => false,
                SAV8BS => true,
                SAV9SV => false,
                _ => false
            };
            menuItem.Visible = spindaAvailable;
            menuItem.Enabled = spindaAvailable;
        }
    }
    
    /// <summary>
    /// Creates a contextual "Spots" button next to the existing cosmetics buttons.
    /// </summary>
    private void CreateSpotsButton()
    {
        try
        {
            if (PKMEditor is not Control editor)
                return;
                
            // Find the History button to place our button next to it
            // This keeps all the appearance buttons together
            var historyButton = FindControl(editor, "BTN_History") as Button;
            if (historyButton?.Parent == null)
                return;

            spotsButton = new Button
            {
                Text = "Spots",
                Name = "BTN_Spots",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                UseVisualStyleBackColor = true,
                Visible = false,
                Margin = new Padding(4, 0, 4, 0),
                Location = new Point(
                    historyButton.Location.X + historyButton.Width + 4,
                    historyButton.Location.Y
                )
            };

            spotsButton.Click += ShowPatternEditor;
            historyButton.Parent.Controls.Add(spotsButton);
        }
        catch
        {
            // If button creation fails, don't crash - the menu will still work
        }
    }
    
    /// <summary>
    /// Looks for a control by name.
    /// </summary>
    /// <param name="parent">Where to start looking.</param>
    /// <param name="name">The control name to find.</param>
    /// <returns>The control if found, null otherwise.</returns>
    private static Control? FindControl(Control parent, string name)
    {
        if (parent.Name == name)
            return parent;
            
        foreach (Control child in parent.Controls)
        {
            var found = FindControl(child, name);
            if (found != null)
                return found;
        }
        
        return null;
    }

    /// <summary>
    /// Shows the Spinda pattern editor dialog.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void ShowPatternEditor(object? sender, EventArgs e)
    {
        var pk = PKMEditor?.Data;
        if (pk == null || !pk.Valid || pk.Species == 0)
        {
            MessageBox.Show("Please select a valid Pokémon first.", Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // If it's not Spinda, offer to make one
        // This lets users try patterns without making a Spinda first
        if (pk.Species != (ushort)Species.Spinda)
        {
            var result = MessageBox.Show(
                "The selected Pokémon is not a Spinda. Do you want to generate a legal Spinda?",
                Name, 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes)
                return;
                
            var spinda = CreateLegalSpinda();
            if (spinda == null)
            {
                MessageBox.Show("Could not generate a legal Spinda for this game version.", Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            pk = spinda;
            PKMEditor?.PopulateFields(pk);
        }

        using var form = new SpindaPatternForm(pk);
        if (form.ShowDialog() == DialogResult.OK)
        {
            PKMEditor?.PopulateFields(pk);
            SaveFileEditor?.ReloadSlots();
        }
    }
    
    /// <summary>
    /// Creates a legal Spinda for the current save file.
    /// </summary>
    private PKM? CreateLegalSpinda()
    {
        try
        {
            var sav = SaveFileEditor.SAV;
            var template = sav.BlankPKM;
            template.Species = (ushort)Species.Spinda;
            template.Form = 0;
            template.Gender = template.GetSaneGender();
            
            // Find valid ways to get Spinda in this game
            // Skip eggs to get a ready-to-use Spinda
            var moves = new ushort[4];
            template.GetMoves(moves);
            var encounters = EncounterMovesetGenerator.GenerateEncounters(template, sav, moves)
                .Where(e => e is not IEncounterEgg)
                .ToList();
            
            if (encounters.Count == 0)
                return null;
            
            var spinda = encounters[0].ConvertToPKM(sav);
            
            // Make sure Spinda matches the save file format
            // Different game versions use different data formats
            var destType = template.GetType();
            if (spinda.GetType() != destType)
            {
                var converted = EntityConverter.ConvertToType(spinda, destType, out var result);
                if (converted != null && result.IsSuccess())
                    spinda = converted;
            }
            
            if (spinda is IHandlerUpdate handler)
                handler.UpdateHandler(sav);
            
            spinda.ResetPartyStats();
            spinda.RefreshChecksum();
            
            return spinda;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Called when a save file is loaded. Updates menu visibility.
    /// </summary>
    public void NotifySaveLoaded()
    {
        if (menuItem != null && SaveFileEditor?.SAV != null)
        {
            // Check if this game has Spinda
            // Hide the plugin for games without Spinda
            bool spindaAvailable = SaveFileEditor.SAV switch
            {
                SAV3 => true,          // Ruby/Sapphire/Emerald/FireRed/LeafGreen
                SAV4 => true,          // Diamond/Pearl/Platinum/HeartGold/SoulSilver
                SAV5 => true,          // Black/White/Black2/White2
                SAV6 => true,          // X/Y/OmegaRuby/AlphaSapphire
                SAV7 => true,          // Sun/Moon/UltraSun/UltraMoon (but not Let's Go)
                SAV7b => false,        // Let's Go Pikachu/Eevee (no Spinda)
                SAV8SWSH => true,      // Sword/Shield
                SAV8LA => false,       // Legends Arceus (no Spinda)
                SAV8BS => true,        // Brilliant Diamond/Shining Pearl
                SAV9SV => false,       // Scarlet/Violet (no Spinda)
                _ => false
            };
            
            // Hide the menu item completely if Spinda doesn't exist in this game
            menuItem.Visible = spindaAvailable;
            menuItem.Enabled = spindaAvailable;
        }
        
        RefreshButtonVisibility();
    }
    
    /// <summary>
    /// Updates the visibility of the Spots button based on the currently selected Pokémon.
    /// </summary>
    private void RefreshButtonVisibility()
    {
        if (spotsButton == null || PKMEditor == null)
            return;
            
        try
        {
            var pk = PKMEditor.Data;
            // Only show the button for Spinda
            // This keeps the UI clean
            bool isSpinda = pk.Species == (ushort)Species.Spinda;
            
            spotsButton.Visible = isSpinda;
        }
        catch
        {
            spotsButton.Visible = false;
        }
    }

    /// <summary>
    /// Attempts to load a file. Not implemented for this plugin.
    /// </summary>
    /// <param name="filePath">The path to the file to load.</param>
    /// <returns>Always returns false as this plugin doesn't handle file loading.</returns>
    public bool TryLoadFile(string filePath)
    {
        return false;
    }
}
