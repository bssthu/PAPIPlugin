using System.Collections.Generic;

namespace PAPIPlugin.Interfaces
{
    public interface ILightTypeManager
    {
        void Initialize(ILightGroup manager);

        IEnumerable<DialogGUIBase> BuildDialogItems();
    }
}
