using Android.Content;
using Android.Views;
using ComoEstamos.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace ComoEstamos
{
    /// <summary>
    /// Handler da <see cref="RolagemComAtalhos"/> no Android: a mesma rolagem do MAUI, mas que olha o toque antes de
    /// passá-lo para as linhas (<see cref="RolagemNativa.DispatchTouchEvent"/>), para ver o arrasto mesmo em cima delas.
    /// </summary>
    public class RolagemComAtalhosHandler : ScrollViewHandler
    {
        protected override MauiScrollView CreatePlatformView() =>
            // Como no ScrollViewHandler do MAUI
            new RolagemNativa(
                new ContextThemeWrapper(MauiContext!.Context, Microsoft.Maui.Resource.Style.scrollViewTheme),
                this)
            {
                ClipToOutline = true,
                FillViewport = true
            };

        private void Avisar(bool paraCima)
        {
            if (VirtualView is not RolagemComAtalhos rolagem)
                return;
            if (paraCima)
                rolagem.AvisarArrastoParaCima();
            else
                rolagem.AvisarArrastoParaBaixo();
        }

        private sealed class RolagemNativa : MauiScrollView
        {
            /// <summary>Distância mínima do arrasto, em dp.</summary>
            private const float DistanciaMinima = 60;

            private readonly RolagemComAtalhosHandler handler;
            private float inicioX, inicioY;
            private bool noFimAoTocar, noTopoAoTocar;

            public RolagemNativa(Context context, RolagemComAtalhosHandler handler)
                : base(context, null!, Microsoft.Maui.Resource.Attribute.scrollViewStyle)
            {
                this.handler = handler;
            }

            public override bool DispatchTouchEvent(MotionEvent? e)
            {
                switch (e?.ActionMasked)
                {
                    case MotionEventActions.Down:
                        inicioX = e.RawX;
                        inicioY = e.RawY;
                        // Vale a posição da rolagem no começo do arrasto: o arrasto que rola até o fim não navega.
                        noFimAoTocar = !CanScrollVertically(1);
                        noTopoAoTocar = !CanScrollVertically(-1);
                        break;

                    case MotionEventActions.Up:
                        var dx = e.RawX - inicioX;
                        var dy = e.RawY - inicioY;
                        var minimo = DistanciaMinima * (Resources?.DisplayMetrics?.Density ?? 1);
                        // Só arrasto vertical: o deslocamento na vertical bem maior que na horizontal
                        if (Math.Abs(dy) >= minimo && Math.Abs(dy) > 2 * Math.Abs(dx))
                        {
                            if (dy < 0 && noFimAoTocar)
                                handler.Avisar(paraCima: true);
                            else if (dy > 0 && noTopoAoTocar)
                                handler.Avisar(paraCima: false);
                        }
                        break;
                }
                return base.DispatchTouchEvent(e);
            }
        }
    }
}
