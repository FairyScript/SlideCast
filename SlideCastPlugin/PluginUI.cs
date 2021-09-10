using Dalamud.Game;
using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SlideCastPlugin
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    internal class PluginUI : IDisposable
    {
        private Configuration configuration;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = true;

        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;

        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        private bool debugVisible = false;

        public bool DebugVisible
        {
            get { return this.debugVisible; }
            set { this.debugVisible = value; }
        }

        private int _cbX;
        private int _cbY;
        private float _cbScale;
        private int _cbCastTime;
        private float _cbCastPer;
        private int _slideTime = 50;
        private Vector4 _slideCol;
        private float _cbCastLast;
        private int _cbCastSameCount;
        private List<byte> _cbSpell = new List<byte>();
        private Colour _colS = new Colour(0.04f, 0.8f, 1f, 1f);
        private readonly Colour _col1S = new Colour(0.04f, 0.4f, 1f, 1f);
        private bool _debug;
        private IntPtr _castBar = IntPtr.Zero;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr GetUi2ObjByNameDelegate(IntPtr getBaseUiObj, string uiName, int index);

        private GetUi2ObjByNameDelegate _getUi2ObjByName;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetBaseUiObjDelegate();

        private GetBaseUiObjDelegate _getBaseUiObj;

        public PluginUI(Configuration configuration, SigScanner sigScanner)
        {
            this.configuration = configuration;

            Visible = configuration.Enabled;
            _slideTime = configuration.SlideTime;
            _slideCol = configuration.SlideCol;

            initHook(sigScanner);
        }

        public void Dispose()
        {
        }

        private void initHook(SigScanner sigScanner)
        {
            var _scan1 = sigScanner.ScanText("E8 ?? ?? ?? ?? 41 b8 01 00 00 00 48 8d 15 ?? ?? ?? ?? 48 8b 48 20 e8 ?? ?? ?? ?? 48 8b cf");
            var _scan2 = sigScanner.ScanText("e8 ?? ?? ?? ?? 48 8b cf 48 89 87 ?? ?? 00 00 e8 ?? ?? ?? ?? 41 b8 01 00 00 00");
            _getBaseUiObj = Marshal.GetDelegateForFunctionPointer<GetBaseUiObjDelegate>(_scan1);
            _getUi2ObjByName = Marshal.GetDelegateForFunctionPointer<GetUi2ObjByNameDelegate>(_scan2);
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawMainWindow();
            DrawSettingsWindow();
            DrawDebugWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }
            var tempCastBar = _getUi2ObjByName(Marshal.ReadIntPtr(_getBaseUiObj(), 0x20), "_CastBar", 1);
            if (tempCastBar == IntPtr.Zero)
            {
                Visible = false;
                return;
            }

            if (_castBar == IntPtr.Zero)
            {
                _castBar = tempCastBar;
            }

            _cbCastLast = _cbCastPer;
            _cbX = Marshal.ReadInt16(_castBar + 0x1BC);
            _cbY = Marshal.ReadInt16(_castBar + 0x1BE);
            _cbScale = Marshal.PtrToStructure<float>(_castBar + 0x1AC);
            _cbCastTime = Marshal.ReadInt16(_castBar + 0x2BC);
            _cbCastPer = Marshal.PtrToStructure<float>(_castBar + 0x2C0);
            var plus = 0;
            _cbSpell = new List<byte>();

            while (Marshal.ReadByte(_castBar + 0x242 + plus) != 0)
            {
                _cbSpell.Add(Marshal.ReadByte(_castBar + 0x242 + plus));
                plus++;
            }

            if (_cbCastLast == _cbCastPer)
            {
                if (_cbCastSameCount < 5)
                { _cbCastSameCount++; }
            }
            else
            {
                _cbCastSameCount = 0;
            }

            if (_cbCastPer == 5)
            {
                _colS = new Colour(_col1S.R / 255f, _col1S.G / 255f, _col1S.B / 255f);
            }

            if (Marshal.ReadByte(_castBar + 0x182).ToString() != "84")
            {
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(_cbX, _cbY));
                ImGui.Begin("SlideCast", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
                ImGui.SetWindowSize(new Vector2(220 * _cbScale, 60 * _cbScale));
                //float time = (float)cbCastTime - (0.01f * cbCastPer * (float)cbCastTime);
                float slidePer = ((float)_cbCastTime - (float)_slideTime) / (float)_cbCastTime;
                ImGui.GetWindowDrawList().AddRectFilled(
                    new Vector2(
                        ImGui.GetWindowPos().X + (48 * _cbScale) + (152 * slidePer * _cbScale),
                        ImGui.GetWindowPos().Y + (20 * _cbScale)),
                    new Vector2(
                        ImGui.GetWindowPos().X + (48 * _cbScale) + 5 + (152 * slidePer * _cbScale),
                        ImGui.GetWindowPos().Y + (29 * _cbScale)),
                    ImGui.GetColorU32(_slideCol));
                ImGui.End();
            }
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(300, 500), ImGuiCond.FirstUseEver);
            ImGui.Begin("SlideCast Config", ref settingsVisible);
            ImGui.Checkbox("Enable", ref visible);
            ImGui.InputInt("Time (cs)", ref _slideTime);
            ImGui.TextWrapped("The time for slidecasting is 50cs (half a second) by default.\n" +
                "Lower numbers make it later in the cast, higher numbers earlier in the cast.\n" +
                "Apart from missed packets, 50cs is the exact safe time to slidecast.");
            ImGui.ColorEdit4("Bar Colour", ref _slideCol, ImGuiColorEditFlags.NoInputs);
            ImGui.Checkbox("Enable Debug Mode", ref _debug);
            ImGui.Separator();
            if (ImGui.Button("Save and Close Config"))
            {
                configuration.Enabled = visible;
                configuration.SlideTime = _slideTime;
                configuration.SlideCol = _slideCol;
                configuration.Save();
                SettingsVisible = false;
            }
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);

            if (ImGui.Button("Buy Haplo a Hot Chocolate"))
            {
                System.Diagnostics.Process.Start("https://ko-fi.com/haplo");
            }
            ImGui.PopStyleColor(3);
            ImGui.End();
        }

        private void DrawDebugWindow()
        {
            if (!DebugVisible)
            {
                return;
            }

            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(_cbX, _cbY));
            ImGui.Begin("SlideCast DEBUG", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
            ImGui.SetWindowSize(new Vector2(220 * _cbScale, 60 * _cbScale));
            //float time = (float)cbCastTime - (0.01f * cbCastPer * (float)cbCastTime);
            var slidePer = ((float)_cbCastTime - (float)_slideTime) / (float)_cbCastTime;
            ImGui.GetWindowDrawList().AddRectFilled(
                new Vector2(
                    ImGui.GetWindowPos().X + (48 * _cbScale) + (152 * slidePer * _cbScale),
                    ImGui.GetWindowPos().Y + (20 * _cbScale)),
                new Vector2(
                    ImGui.GetWindowPos().X + (48 * _cbScale) + 5 + (152 * slidePer * _cbScale),
                    ImGui.GetWindowPos().Y + (29 * _cbScale)),
                ImGui.GetColorU32(_slideCol));
            ImGui.End();

            ImGui.Begin("Slidecast Debug Values");
            ImGui.Text("cbX: " + _cbX);
            ImGui.Text("cbY: " + _cbY);
            ImGui.Text("cbS: " + _cbScale);
            ImGui.Text("cbCastTime: " + _cbCastTime);
            ImGui.Text("cbCastPer: " + _cbCastPer);
            ImGui.Text("Mem Addr: " + _castBar.ToString("X"));
            ImGui.Text(_colS.Hue.ToString());
            ImGui.Text(_colS.Saturation.ToString());
            ImGui.Text(_colS.Brightness.ToString());
            ImGui.End();
        }
    }
}