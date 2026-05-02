#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using PAPIPlugin.Interfaces;

#endregion

namespace PAPIPlugin.UI
{
    public class SettingsPopupDialog
    {
        private const string DialogName = "PAPIPlugin.Settings";

        private readonly Func<ILightArrayConfig> _getConfig;

        private readonly Action _saveAll;

        private readonly Action _reloadAll;

        private PopupDialog _popupDialog;

        public SettingsPopupDialog(Func<ILightArrayConfig> getConfig, Action saveAll, Action reloadAll)
        {
            _getConfig = getConfig;
            _saveAll = saveAll;
            _reloadAll = reloadAll;
        }

        public void ToggleVisible()
        {
            if (_popupDialog == null)
            {
                Show();
                return;
            }

            Dismiss();
        }

        public void Show()
        {
            Dismiss();

            var dialog = BuildDialog();
            _popupDialog = PopupDialog.SpawnPopupDialog(dialog, false, HighLogic.UISkin, false);
            if (_popupDialog != null)
            {
                _popupDialog.OnDismiss = HandleDismiss;
                _popupDialog.SetDraggable(true);
            }
        }

        public void Dismiss()
        {
            if (_popupDialog == null)
            {
                return;
            }

            var popupDialog = _popupDialog;
            _popupDialog = null;
            popupDialog.Dismiss();
        }

        private void HandleDismiss()
        {
            _popupDialog = null;
        }

        private MultiOptionDialog BuildDialog()
        {
            var dialogItems = new List<DialogGUIBase>();
            var config = _getConfig();

            if (config != null)
            {
                foreach (var lightGroup in config.LightArrayGroups.Where(lightGroup => !string.IsNullOrEmpty(lightGroup.Name)))
                {
                    dialogItems.Add(new DialogGUILabel(lightGroup.Name, true, false));
                    dialogItems.Add(new DialogGUIVerticalLayout(lightGroup.BuildDialogItems().ToArray()));
                }
            }

            dialogItems.Add(new DialogGUIHorizontalLayout(
                new DialogGUIButton("Save All", () => _saveAll(), 140f, 30f, false),
                new DialogGUIButton("Reload All", () => _reloadAll(), 140f, 30f, false),
                new DialogGUIButton("Close", Dismiss, 100f, 30f, true)));

            return new MultiOptionDialog(DialogName, string.Empty, "PAPIPlugin Settings", HighLogic.UISkin, 520f,
                new DialogGUIVerticalLayout(dialogItems.ToArray()));
        }
    }
}