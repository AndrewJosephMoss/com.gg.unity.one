using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Buttons
{
    [UxmlElement]
    public abstract partial class LoadingButton : Button
    {
        const string LoadingClass = "menu-button--loading";
        const string MenuButtonClass = "menu-button";

        bool _loading;

        [UxmlAttribute("loading")]
        public bool Loading
        {
            get => _loading;
            set => SetLoading(value);
        }

        protected LoadingButton()
        {
            AddToClassList(MenuButtonClass);
        }

        public void SetLoading(bool loading)
        {
            if (_loading == loading) 
                return;
            _loading = loading;

            EnableInClassList(LoadingClass, loading);
            pickingMode = loading ? PickingMode.Ignore : PickingMode.Position;
            
            focusable = !loading;

            if (loading) 
                Blur();

            OnLoadingChanged(loading);
        }

        protected abstract void OnLoadingChanged(bool loading);
    }
}

