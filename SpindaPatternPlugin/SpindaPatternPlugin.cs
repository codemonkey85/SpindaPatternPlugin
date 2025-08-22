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
    public string Name => "Spinda Pattern Editor";
    public int Priority => 1;

    public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
    public IPKMView PKMEditor { get; private set; } = null!;
    private ToolStripMenuItem? menuItem;
    private Button? spotsButton;

    public void Initialize(params object[] args)
    {
        SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider)!;
        PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView)!;
        var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip)!;
        SetupMenu(menu);
        
        CreateSpotsButton();
        
        if (PKMEditor is Control control)
        {
            control.Enter += (_, _) => RefreshButtonVisibility();
            control.Leave += (_, _) => RefreshButtonVisibility();
            
            var speciesControl = FindControl(control, "CB_Species");
            if (speciesControl is ComboBox speciesCombo)
            {
                speciesCombo.SelectedIndexChanged += (_, _) => RefreshButtonVisibility();
            }
        }
    }

    private void SetupMenu(ToolStrip menuStrip)
    {
        var items = menuStrip.Items;
        if (items.Find("Menu_Tools", false)[0] is not ToolStripDropDownItem tools)
            throw new ArgumentException(null, nameof(menuStrip));
        
        menuItem = new ToolStripMenuItem(Name)
        {
            ShortcutKeys = Keys.Control | Keys.Shift | Keys.S
        };
        menuItem.Click += ShowPatternEditor;
        tools.DropDownItems.Add(menuItem);
        
        // Initial visibility check if a save is already loaded
        if (SaveFileEditor?.SAV != null)
        {
            bool spindaAvailable = SaveFileEditor.SAV.Personal.IsSpeciesInGame((ushort)Species.Spinda);
            menuItem.Visible = spindaAvailable;
            menuItem.Enabled = spindaAvailable;
        }
    }
    
    private void CreateSpotsButton()
    {
        try
        {
            if (PKMEditor is not Control editor)
                return;
                
            var historyButton = FindControl(editor, "BTN_History") as Button;
            if (historyButton?.Parent == null)
                return;
            
            spotsButton = new Button
            {
                Text = "Spots",
                Name = "BTN_Spots",
                Size = new Size(60, 27),
                UseVisualStyleBackColor = true,
                Visible = false
            };
            
            spotsButton.Location = new Point(
                historyButton.Location.X + historyButton.Width + 4,
                historyButton.Location.Y
            );
            
            spotsButton.Click += ShowPatternEditor;
            historyButton.Parent.Controls.Add(spotsButton);
        }
        catch
        {
        }
    }
    
    private Control? FindControl(Control parent, string name)
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

    private void ShowPatternEditor(object? sender, EventArgs e)
    {
        var pk = PKMEditor.Data;
        if (!pk.Valid || pk.Species == 0)
        {
            MessageBox.Show("Please select a valid Pokémon first.", Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

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
            PKMEditor.PopulateFields(pk);
        }

        using var form = new SpindaPatternForm(pk);
        if (form.ShowDialog() == DialogResult.OK)
        {
            PKMEditor.PopulateFields(pk);
            SaveFileEditor.ReloadSlots();
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
            
            var moves = new ushort[4];
            template.GetMoves(moves);
            var encounters = EncounterMovesetGenerator.GenerateEncounters(template, sav, moves)
                .Where(e => e is not IEncounterEgg)
                .ToList();
            
            if (encounters.Count == 0)
                return null;
            
            var spinda = encounters[0].ConvertToPKM(sav);
            
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

    public void NotifySaveLoaded()
    {
        if (menuItem != null && SaveFileEditor.SAV != null)
        {
            // Check if Spinda is available in this game using PKHeX's PersonalTable
            bool spindaAvailable = SaveFileEditor.SAV.Personal.IsSpeciesInGame((ushort)Species.Spinda);
            
            // Hide the menu item completely if Spinda doesn't exist in this game
            menuItem.Visible = spindaAvailable;
            menuItem.Enabled = spindaAvailable;
        }
        
        RefreshButtonVisibility();
    }
    
    private void RefreshButtonVisibility()
    {
        if (spotsButton == null)
            return;
            
        try
        {
            var pk = PKMEditor.Data;
            // Only show button if:
            // 1. Current Pokémon is Spinda
            // 2. Spinda exists in this game's data
            bool isSpinda = pk.Species == (ushort)Species.Spinda;
            bool spindaInGame = SaveFileEditor?.SAV?.Personal.IsSpeciesInGame((ushort)Species.Spinda) ?? false;
            
            spotsButton.Visible = isSpinda && spindaInGame;
        }
        catch
        {
            spotsButton.Visible = false;
        }
    }

    public bool TryLoadFile(string filePath)
    {
        return false;
    }
}
