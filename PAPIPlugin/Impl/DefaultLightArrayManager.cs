#region Usings

using System;
using System.Diagnostics;
using System.Linq;
using PAPIPlugin.Interfaces;
using PAPIPlugin.Internal;
using PAPIPlugin.UI;
using UnityEngine;

#endregion

namespace PAPIPlugin.Impl
{
    public class DefaultLightArrayManager : ILightArrayManager
    {
        private KSP.UI.Screens.ApplicationLauncherButton _appButtonStock = null;

        private SettingsPopupDialog _settingsDialog;

        private ILightArrayConfig _lightConfig;

        public DefaultLightArrayManager()
        {
        }

        #region ILightArrayManager Members

        public event EventHandler ParsingFinished;

        public event EventHandler AllLightConfigReloaded;

        public ILightArrayConfig LightConfig
        {
            get { return _lightConfig; }
            set
            {
                if (Equals(_lightConfig, value))
                {
                    return;
                }

                _lightConfig = value;

                InitializeConfig(_lightConfig);
            }
        }

        public ILightArrayConfig LoadConfig()
        {
            Util.LogInfo("Starting to parse light definitions...");

            var stopwatch = Stopwatch.StartNew();

            var defaultConfig = new DefaultLightArrayConfig();
            defaultConfig.LoadConfig();

            LightConfig = defaultConfig;

            Util.LogInfo(string.Format("Finished parsing definitions. Found {0} light groups with a total of {1} light arrays in a time of {2}.",
                LightConfig.LightArrayGroups.Count(), LightConfig.LightArrayGroups.Sum(group => group.LightArrays.Count()), stopwatch.Elapsed));

            OnParsingFinished();

            return LightConfig;
        }

        public void SaveConfig()
        {
            if (LightConfig != null)
            {
                var config = LightConfig as DefaultLightArrayConfig;
                config.SaveConfig();
            }
        }

        public void InitializeButton()
        {
            if (LightConfig == null)
            {
                return;
            }

            if (_appButtonStock == null)
            {
                OnGUIAppLauncherReady();
            }
        }

        public void Update()
        {
            if (LightConfig == null)
            {
                return;
            }

            if (_appButtonStock == null)
            {
                InitializeButton();
            }

            foreach (var lightGroup in LightConfig.LightArrayGroups)
            {
                lightGroup.Update();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        private void OnGUIAppLauncherReady()
        {
            if (_appButtonStock != null)
            {
                return;
            }

            if (!KSP.UI.Screens.ApplicationLauncher.Ready)
            {
                return;
            }

            var iconTexture = (Texture)GameDatabase.Instance.GetTexture("PAPIPlugin/icon_button", false);
            if (iconTexture == null)
            {
                Util.LogWarning("Failed to load the stock launcher icon texture PAPIPlugin/icon_button.");
            }

            _appButtonStock = KSP.UI.Screens.ApplicationLauncher.Instance.AddModApplication(
                OnIconClickHandler,
                OnIconClickHandler,
                DummyVoid,
                DummyVoid,
                DummyVoid,
                DummyVoid,
                KSP.UI.Screens.ApplicationLauncher.AppScenes.FLIGHT | KSP.UI.Screens.ApplicationLauncher.AppScenes.SPACECENTER,
                iconTexture
            );

            if (_appButtonStock != null)
            {
                Util.LogInfo("Registered stock launcher button.");
            }
        }

        private void DummyVoid() { }

        private void OnIconClickHandler()
        {
            if (_settingsDialog == null)
            {
                _settingsDialog = new SettingsPopupDialog(() => LightConfig, SaveConfig, ReloadConfigAndRefreshDialog);
            }

            _settingsDialog.ToggleVisible();

            if (_appButtonStock != null)
            {
                // Don't lock highlight on the button since it's just a toggle
                _appButtonStock.SetFalse(false);
            }
        }

        private void ReloadConfigAndRefreshDialog()
        {
            if (_settingsDialog != null)
            {
                _settingsDialog.Dismiss();
            }

            if (LightConfig != null)
            {
                LightConfig.Destroy();
            }

            LoadConfig();
            AllLightConfigReloaded?.Invoke(this, EventArgs.Empty);

            if (_settingsDialog != null)
            {
                _settingsDialog.Show();
            }
        }

        private void InitializeConfig(ILightArrayConfig lightConfig)
        {
            if (lightConfig == null)
            {
                return;
            }

            foreach (var lightArray in lightConfig.LightArrayGroups.SelectMany(group => group.LightArrays))
            {
                lightArray.InitializeDisplay(this);
            }
        }

        protected virtual void OnParsingFinished()
        {
            var handler = ParsingFinished;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        ~DefaultLightArrayManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (LightConfig != null)
            {
                LightConfig.Destroy();
            }

            if (_appButtonStock != null)
            {
                KSP.UI.Screens.ApplicationLauncher.Instance.RemoveModApplication(_appButtonStock);
                _appButtonStock = null;
            }

            if (_settingsDialog != null)
            {
                _settingsDialog.Dismiss();
                _settingsDialog = null;
            }
        }
    }
}
