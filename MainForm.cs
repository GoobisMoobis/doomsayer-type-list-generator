using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;

namespace DoomsayerTypeListGenerator;

public class MainForm : Form
{
    private readonly Panel topPanel;
    private readonly TextBox pathBox;
    private readonly Button browsePathBtn;
    private readonly CheckedListBox dllList;
    private readonly Button addDllBtn;
    private readonly Button removeDllBtn;
    private readonly Button generateBtn;
    private readonly Label statusLabel;
    private readonly RichTextBox outputBox;

    private readonly List<string> _dllPaths = new();
    private int _fixedCount = 0;
    private string? _baseDllPath;

    public MainForm()
    {
        Text = "Doomsayer Type List Generator";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Size = new Size(800, 680);
        MinimumSize = new Size(600, 560);
        Font = new Font("Segoe UI", 9f);
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 218,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        var pathLabel = new Label
        {
            Text = "Among Us Path:",
            AutoSize = true,
            Location = new Point(12, 12),
            ForeColor = Color.Silver
        };

        pathBox = new TextBox
        {
            Location = new Point(12, 30),
            Width = 540,
            BackColor = Color.FromArgb(55, 55, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9f)
        };

        browsePathBtn = new Button
        {
            Text = "Browse…",
            Location = new Point(560, 28),
            Width = 80,
            Height = 26,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        browsePathBtn.FlatAppearance.BorderColor = Color.Gray;
        browsePathBtn.Click += OnBrowsePath;

        var dllLabel = new Label
        {
            Text = "DLLs to inspect:",
            AutoSize = true,
            Location = new Point(12, 64),
            ForeColor = Color.Silver
        };

        dllList = new CheckedListBox
        {
            Location = new Point(12, 82),
            Width = 624,
            Height = 88,
            BackColor = Color.FromArgb(55, 55, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9f),
            CheckOnClick = true
        };

        addDllBtn = new Button
        {
            Text = "Add…",
            Location = new Point(644, 82),
            Width = 80,
            Height = 26,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        addDllBtn.FlatAppearance.BorderColor = Color.Gray;
        addDllBtn.Click += OnAddDll;

        removeDllBtn = new Button
        {
            Text = "Remove",
            Location = new Point(644, 114),
            Width = 80,
            Height = 26,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        removeDllBtn.FlatAppearance.BorderColor = Color.Gray;
        removeDllBtn.Click += OnRemoveDll;

        generateBtn = new Button
        {
            Text = "Generate List",
            Location = new Point(12, 182),
            Width = 140,
            Height = 28,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        generateBtn.FlatAppearance.BorderSize = 0;
        generateBtn.Click += OnGenerate;

        statusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(162, 182),
            Height = 28,
            ForeColor = Color.Silver,
            Text = "",
            TextAlign = ContentAlignment.MiddleLeft
        };

        topPanel.Controls.AddRange(new Control[]
        {
            pathLabel, pathBox, browsePathBtn,
            dllLabel, dllList, addDllBtn, removeDllBtn,
            generateBtn, statusLabel
        });

        topPanel.Resize += OnTopPanelResize;

        outputBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.White,
            Font = new Font("Consolas", 9.5f),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Padding = new Padding(8)
        };

        Controls.Add(outputBox);
        Controls.Add(topPanel);

        DetectPath();
    }

    private void OnTopPanelResize(object? sender, EventArgs e)
    {
        var w = topPanel.ClientSize.Width;
        pathBox.Width = w - 156;
        browsePathBtn.Left = w - 92;
        dllList.Width = w - 168;
        addDllBtn.Left = w - 80 - 12;
        removeDllBtn.Left = w - 80 - 12;
        statusLabel.Width = w - 174;
    }

    // Path Detection
    private void DetectPath()
    {
        var found = TryFindAmongUs();
        if (found != null)
        {
            pathBox.Text = found;
            RefreshDllsForPath(found);
            SetStatus("✓ Among Us installation detected automatically.", Color.LightGreen);
        }
        else
        {
            SetStatus("⚠ Could not detect Among Us — please Browse to your install folder.", Color.Orange);
        }
    }

    private void RefreshDllsForPath(string amongUsPath)
    {
        dllList.Items.Clear();
        _dllPaths.Clear();
        _fixedCount = 0;
        _baseDllPath = null;

        var pluginsDir = Path.Combine(amongUsPath, "BepInEx", "plugins");
        if (!Directory.Exists(pluginsDir)) return;

        var touMiraDll = Directory
            .GetFiles(pluginsDir, "TownOfUsMira.dll", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (touMiraDll != null)
        {
            _baseDllPath = touMiraDll;
            dllList.Items.Add("TownOfUsMira.dll  [base]", isChecked: true);
            _dllPaths.Add(touMiraDll);
            _fixedCount++;
        }

        var remDll = Directory
            .GetFiles(pluginsDir, "TouMiraRolesExtension.dll", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (remDll != null)
        {
            dllList.Items.Add("TouMiraRolesExtension.dll", isChecked: true);
            _dllPaths.Add(remDll);
            _fixedCount++;
        }
    }

    private static bool IsValidAmongUsInstall(string path)
    {
        try
        {
            if (!File.Exists(Path.Combine(path, "Among Us.exe"))) return false;
            var pluginsDir = Path.Combine(path, "BepInEx", "plugins");
            if (!Directory.Exists(pluginsDir)) return false;
            return Directory.GetFiles(pluginsDir, "TownOfUsMira.dll", SearchOption.AllDirectories).Length > 0;
        }
        catch { return false; }
    }

    // Browse
    private void OnBrowsePath(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select Among Us.exe in your Among Us install folder",
            Filter = "Among Us|Among Us.exe|All executables|*.exe",
            CheckFileExists = true,
            CheckPathExists = true,
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        var folder = Path.GetDirectoryName(dlg.FileName);
        if (folder == null) return;

        pathBox.Text = folder;

        var pluginsDir = Path.Combine(folder, "BepInEx", "plugins");
        if (!Directory.Exists(Path.Combine(folder, "BepInEx")))
            SetStatus("⚠ No BepInEx folder found; is BepInEx installed?", Color.Orange);
        else if (!Directory.Exists(pluginsDir))
            SetStatus("⚠ BepInEx found but no plugins folder exists.", Color.Orange);
        else if (Directory.GetFiles(pluginsDir, "TownOfUsMira.dll", SearchOption.AllDirectories).Length == 0)
            SetStatus("⚠ BepInEx found but TownOfUsMira.dll not in plugins; is TownOfUs Mira installed?", Color.Orange);
        else
            SetStatus("✓ Path set", Color.LightGreen);

        RefreshDllsForPath(folder);
    }

    private void OnAddDll(object? sender, EventArgs e)
    {
        var initialDir = string.Empty;
        var basePath = pathBox.Text.Trim();
        if (Directory.Exists(basePath))
        {
            var candidate = Path.Combine(basePath, "BepInEx", "plugins");
            if (Directory.Exists(candidate))
                initialDir = candidate;
        }

        using var dlg = new OpenFileDialog
        {
            Title = "Select a plugin DLL to add",
            Filter = "DLL files|*.dll|All files|*.*",
            CheckFileExists = true,
            CheckPathExists = true,
            InitialDirectory = initialDir,
            Multiselect = true
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        foreach (var file in dlg.FileNames)
        {
            if (_dllPaths.Contains(file, StringComparer.OrdinalIgnoreCase)) continue;
            dllList.Items.Add(Path.GetFileName(file), isChecked: true);
            _dllPaths.Add(file);
        }
    }

    private void OnRemoveDll(object? sender, EventArgs e)
    {
        var toRemove = dllList.SelectedIndices
            .Cast<int>()
            .Where(i => i >= _fixedCount)
            .OrderByDescending(i => i)
            .ToList();

        foreach (var i in toRemove)
        {
            dllList.Items.RemoveAt(i);
            _dllPaths.RemoveAt(i);
        }

        if (toRemove.Count == 0 && dllList.SelectedIndex >= 0)
            SetStatus("⚠ Fixed entries (TOU:M, REM) cannot be removed — uncheck them instead.", Color.Orange);
    }

    /// <summary>
    /// Returnws the Among Us install path if it can be found, otherwise null.
    /// Checks Steam, Xbox, and Itch.io locations.
    /// </summary>
    private static string? TryFindAmongUs()
    {
        // Steam
        var steamApps = @"C:\Program Files (x86)\Steam\steamapps\common";
        if (Directory.Exists(steamApps))
        {
            var found = Directory.GetDirectories(steamApps)
                .FirstOrDefault(IsValidAmongUsInstall);

            if (found != null)
                return found;
        }

        // Xbox
        var xboxGames = @"C:\XboxGames";
        if (Directory.Exists(xboxGames))
        {
            var found = Directory.GetDirectories(xboxGames)
                .Select(dir => Path.Combine(dir, "Content"))
                .FirstOrDefault(dir => Directory.Exists(dir) && IsValidAmongUsInstall(dir));

            if (found != null)
                return found;
        }

        // Itch.io
        var itchAppData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "itch", "apps");

        if (Directory.Exists(itchAppData))
        {
            foreach (var dir in Directory.GetDirectories(itchAppData))
            {
                if (IsValidAmongUsInstall(dir))
                    return dir;

                try
                {
                    var found = Directory.GetDirectories(dir)
                        .FirstOrDefault(IsValidAmongUsInstall);

                    if (found != null)
                        return found;
                }
                catch
                {
                    // Ignored
                }
            }
        }

        return null;
    }

    // Generate
    private void OnGenerate(object? sender, EventArgs e)
    {
        outputBox.Clear();

        var checkedPaths = dllList.CheckedIndices
            .Cast<int>()
            .Select(i => _dllPaths[i])
            .Where(File.Exists)
            .ToList();

        if (checkedPaths.Count == 0)
        {
            SetStatus("❌ No DLLs selected or none found on disk.", Color.OrangeRed);
            return;
        }

        SetStatus($"⏳ Inspecting {checkedPaths.Count} DLL(s)…", Color.Silver);
        generateBtn.Enabled = false;
        Application.DoEvents();

        try
        {
            var allRoles = new List<RoleInfo>();
            var allEnumValues = new Dictionary<int, string>();

            foreach (var path in checkedPaths)
            {
                var (roles, enumValues) = InspectAssembly(path);
                allRoles.AddRange(roles);
                foreach (var kv in enumValues)
                    allEnumValues.TryAdd(kv.Key, kv.Value);
            }

            if (allEnumValues.Count == 0)
                Append("⚠  Could not locate DoomableType enum, showing raw int values.\n\n", Color.Orange);

            if (allRoles.Count == 0)
            {
                Append("No IDoomable roles found in target namespaces.\n", Color.OrangeRed);
                SetStatus("Found no results.", Color.Silver);
                return;
            }

            var grouped = allRoles.GroupBy(r => r.DoomTypeValue).OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var groupLabel = allEnumValues.TryGetValue(group.Key, out var eName)
                    ? eName : $"Unknown ({group.Key})";

                Append($"── {groupLabel} ", Color.FromArgb(100, 180, 255));
                Append($"({group.Count()} role{(group.Count() == 1 ? "" : "s")})\n", Color.Gray);

                var byNs = group
                    .GroupBy(r => r.NamespaceShort)
                    .OrderBy(g => g.Key);

                foreach (var nsGroup in byNs)
                {
                    Append($"  [{nsGroup.Key}]\n", Color.FromArgb(180, 180, 100));

                    foreach (var role in nsGroup.OrderBy(r => r.TypeName))
                    {
                        var displayName = role.TypeName.EndsWith("Role")
                            ? role.TypeName[..^4]
                            : role.TypeName;

                        bool isBase = string.Equals(role.SourceDll, _baseDllPath, StringComparison.OrdinalIgnoreCase);

                        if (isBase)
                        {
                            Append($"    • {displayName}\n", Color.White);
                        }
                        else
                        {
                            var originName = Path.GetFileNameWithoutExtension(role.SourceDll);
                            Append($"    • {displayName} ", Color.FromArgb(255, 200, 100));
                            Append($"(Origin: {originName})\n", Color.FromArgb(160, 130, 70));
                        }
                    }
                }

                Append("\n", Color.White);
            }

            SetStatus($"✓ Found {allRoles.Count} IDoomable role(s) across {grouped.Count()} DoomableType value(s).", Color.LightGreen);
        }
        catch (Exception ex)
        {
            SetStatus("❌ Error during inspection.", Color.OrangeRed);
            Append($"\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", Color.OrangeRed);
        }
        finally
        {
            generateBtn.Enabled = true;
        }
    }

    // Assembly Inspector

    private sealed record RoleInfo(string TypeName, string Namespace, string NamespaceShort, string SourceDll, int DoomTypeValue);

    private static (List<RoleInfo> roles, Dictionary<int, string> enumValues) InspectAssembly(string dllPath)
    {
        var enumValues = TryReadEnum(dllPath, "DoomableType");
        var roles = new List<RoleInfo>();

        using var stream = File.OpenRead(dllPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata) return (roles, enumValues);

        var reader = peReader.GetMetadataReader();

        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var typeDef = reader.GetTypeDefinition(typeHandle);
            var typeName = reader.GetString(typeDef.Name);
            if (typeName.Contains('<') || typeName.Contains('>')) continue;

            var ns = reader.GetString(typeDef.Namespace);
            var nsShort = ns.Contains('.') ? ns.Split('.').Last() : ns;

            bool isDoomable = typeDef.GetInterfaceImplementations().Any(ih =>
                ResolveTypeName(reader, reader.GetInterfaceImplementation(ih).Interface) == "IDoomable");

            if (!isDoomable) continue;

            int? value = FindDoomHintTypeValue(reader, peReader, typeDef)
                         ?? WalkBaseTypes(reader, peReader, typeDef);

            if (value.HasValue)
                roles.Add(new RoleInfo(typeName, ns, nsShort, dllPath, value.Value));
        }

        return (roles, enumValues);
    }

    private static int? FindDoomHintTypeValue(MetadataReader reader, PEReader peReader, TypeDefinition typeDef)
    {
        foreach (var propHandle in typeDef.GetProperties())
        {
            var prop = reader.GetPropertyDefinition(propHandle);
            if (reader.GetString(prop.Name) != "DoomHintType") continue;
            var accessors = prop.GetAccessors();
            if (!accessors.Getter.IsNil)
                return ExtractConstantReturn(reader, peReader, accessors.Getter);
        }

        foreach (var methodHandle in typeDef.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            if (reader.GetString(method.Name).EndsWith("get_DoomHintType", StringComparison.Ordinal))
                return ExtractConstantReturn(reader, peReader, methodHandle);
        }

        return null;
    }

    private static int? WalkBaseTypes(MetadataReader reader, PEReader peReader, TypeDefinition typeDef)
    {
        var baseHandle = typeDef.BaseType;
        if (baseHandle.IsNil || baseHandle.Kind != HandleKind.TypeDefinition) return null;
        var baseDef = reader.GetTypeDefinition((TypeDefinitionHandle)baseHandle);
        return FindDoomHintTypeValue(reader, peReader, baseDef) ?? WalkBaseTypes(reader, peReader, baseDef);
    }

    private static string ResolveTypeName(MetadataReader reader, EntityHandle handle) =>
        handle.Kind switch
        {
            HandleKind.TypeReference => reader.GetString(reader.GetTypeReference((TypeReferenceHandle)handle).Name),
            HandleKind.TypeDefinition => reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)handle).Name),
            _ => string.Empty
        };

    private static int? ExtractConstantReturn(MetadataReader reader, PEReader peReader, MethodDefinitionHandle methodHandle)
    {
        try
        {
            var rva = reader.GetMethodDefinition(methodHandle).RelativeVirtualAddress;
            if (rva == 0) return null;
            var il = peReader.GetMethodBody(rva).GetILBytes();
            for (int i = 0; i < il.Length; i++)
            {
                switch (il[i])
                {
                    case 0x15: return -1;
                    case 0x16: return 0;
                    case 0x17: return 1;
                    case 0x18: return 2;
                    case 0x19: return 3;
                    case 0x1A: return 4;
                    case 0x1B: return 5;
                    case 0x1C: return 6;
                    case 0x1D: return 7;
                    case 0x1E: return 8;
                    case 0x1F when i + 1 < il.Length: return (sbyte)il[i + 1];
                    case 0x20 when i + 4 < il.Length: return BitConverter.ToInt32(il, i + 1);
                }
            }
        }
        catch
        {
            //
        }
        return null;
    }

    private static Dictionary<int, string> TryReadEnum(string dllPath, string enumName)
    {
        using var stream = File.OpenRead(dllPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata) return new Dictionary<int, string>();

        var reader = peReader.GetMetadataReader();
        var result = new Dictionary<int, string>();

        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var typeDef = reader.GetTypeDefinition(typeHandle);
            if (reader.GetString(typeDef.Name) != enumName) continue;

            foreach (var fieldHandle in typeDef.GetFields())
            {
                var field = reader.GetFieldDefinition(fieldHandle);
                var fieldName = reader.GetString(field.Name);
                if (fieldName == "value__") continue;
                var constHandle = field.GetDefaultValue();
                if (constHandle.IsNil) continue;
                var blobReader = reader.GetBlobReader(reader.GetConstant(constHandle).Value);
                result[blobReader.ReadInt32()] = fieldName;
            }

            if (result.Count > 0) return result;
        }

        return result;
    }

    // Helpers

    private void Append(string text, Color color)
    {
        outputBox.SelectionStart = outputBox.TextLength;
        outputBox.SelectionLength = 0;
        outputBox.SelectionColor = color;
        outputBox.AppendText(text);
    }

    private void SetStatus(string text, Color color)
    {
        statusLabel.Text = text;
        statusLabel.ForeColor = color;
    }
}