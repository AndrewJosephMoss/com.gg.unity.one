using System;
using GG.Unity.UserInterface;

namespace GG.Unity.UserInterface.Buttons
{
    [Serializable]
    public class LoadingButtonBinding : ButtonBindingBase<LoadingButton>
    {
        bool _loading;

        public bool Loading => _loading;

        public override void Bind(UnityEngine.UIElements.VisualElement root, UnityEngine.Object context)
        {
            base.Bind(root, context);

            // A UI reload rebuilds the tree, so reapply the remembered state
            if (Element != null && _loading)
                Element.SetLoading(true);
        }

        public void SetLoading(bool loading)
        {
            _loading = loading;
            Element?.SetLoading(loading);
        }
    }
}
