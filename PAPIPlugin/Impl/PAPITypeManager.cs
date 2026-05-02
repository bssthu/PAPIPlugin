#region Usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PAPIPlugin.Arrays;
using PAPIPlugin.Interfaces;
using PAPIPlugin.Internal;

#endregion

namespace PAPIPlugin.Impl
{
    public class PAPITypeManager : ILightTypeManager
    {
        private readonly IList<PAPIArray> _papiArrays = new List<PAPIArray>();

        private double _initialGlideslopeValue = PAPIArray.DefaultTargetGlidePath;

        private double _initialTargetGlideslopeValue = PAPIArray.DefaultGlideslopeTolerance;

        private string _glideslopeText;

        private string _glideslopeToleranceText;

        private string _validationMessage = string.Empty;

        #region ILightTypeManager Members

        public void Initialize(ILightGroup group)
        {
            Util.LogInfo(group);

            foreach (var lightArray in group.LightArrays.OfType<PAPIArray>())
            {
                _papiArrays.Add(lightArray);

                _initialTargetGlideslopeValue = lightArray.GlideslopeTolerance;
                _initialGlideslopeValue = lightArray.TargetGlideslope;
            }

            group.LightArrayAdded += (sender, arguments) =>
                {
                    var papi = arguments.Array as PAPIArray;
                    if (papi == null)
                    {
                        return;
                    }

                    _papiArrays.Add(papi);

                    _initialTargetGlideslopeValue = papi.GlideslopeTolerance;
                    _initialGlideslopeValue = papi.TargetGlideslope;
                };
        }

        public IEnumerable<DialogGUIBase> BuildDialogItems()
        {
            EnsureDialogState();

            return new DialogGUIBase[]
            {
                CreateDegreeInputRow("Glideslope", _glideslopeText, UpdateGlideslopeText),
                CreateDegreeInputRow("Glideslope tolerance", _glideslopeToleranceText, UpdateGlideslopeToleranceText),
                new DialogGUILabel(() => _validationMessage, true, false)
            };
        }

        #endregion

        private void EnsureDialogState()
        {
            if (_papiArrays.Count > 0)
            {
                _glideslopeText = _papiArrays[0].TargetGlideslope.ToString(CultureInfo.InvariantCulture);
                _glideslopeToleranceText = _papiArrays[0].GlideslopeTolerance.ToString(CultureInfo.InvariantCulture);
                return;
            }

            if (string.IsNullOrEmpty(_glideslopeText))
            {
                _glideslopeText = _initialGlideslopeValue.ToString(CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrEmpty(_glideslopeToleranceText))
            {
                _glideslopeToleranceText = _initialTargetGlideslopeValue.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static DialogGUIBase CreateDegreeInputRow(string label, string currentValue, Func<string, string> onTextUpdated)
        {
            return new DialogGUIHorizontalLayout(
                new DialogGUILabel(label, 180f, 30f),
                new DialogGUITextInput(currentValue, false, 24, onTextUpdated, 120f, 30f),
                new DialogGUILabel("deg", 40f, 30f));
        }

        private string UpdateGlideslopeText(string input)
        {
            _glideslopeText = input;
            return UpdateAllArrays(input, (papiArray, value) => papiArray.TargetGlideslope = value, "Invalid glideslope value.");
        }

        private string UpdateGlideslopeToleranceText(string input)
        {
            _glideslopeToleranceText = input;
            return UpdateAllArrays(input, (papiArray, value) => papiArray.GlideslopeTolerance = value, "Invalid glideslope tolerance value.");
        }

        private string UpdateAllArrays(string input, Action<PAPIArray, double> applyValue, string errorMessage)
        {
            double parsedValue;
            if (!TryParseDouble(input, out parsedValue))
            {
                _validationMessage = errorMessage;
                return input;
            }

            _validationMessage = string.Empty;
            foreach (var papiArray in _papiArrays)
            {
                applyValue(papiArray, parsedValue);
            }

            return input;
        }

        private static bool TryParseDouble(string input, out double value)
        {
            return double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) ||
                   double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
        }

    }
}
