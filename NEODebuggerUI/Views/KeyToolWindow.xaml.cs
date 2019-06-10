using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;
using NEODebuggerUI.ViewModels;
using NEODebuggerUI.Models;
using Neo.Debugger.Core.Utils;

namespace NEODebuggerUI.Views
{
    public class KeyToolWindow : ReactiveWindow<KeyToolWindowViewModel>
    {
        public KeyToolWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            RenderKeyDataGrid("");

            this.ViewModel.EvtPrivateKeyChanged += (privateKey) => RenderKeyDataGrid(ViewModel.PrivateKey);

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void RenderKeyDataGrid(string key)
        {
            var grid = this.FindControl<Grid>("KeyDataGrid");
            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            var rowHeader = new RowDefinition { Height = new GridLength(20) };
            grid.RowDefinitions.Add(rowHeader);

            var propertyHeader = new TextBlock { Text = "Property", FontWeight = FontWeight.Bold };
            Grid.SetRow(propertyHeader, 0);
            Grid.SetColumn(propertyHeader, 0);
            grid.Children.Add(propertyHeader);

            var valueHeader = new TextBlock { Text = "Value", FontWeight = FontWeight.Bold };
            Grid.SetRow(valueHeader, 0);
            Grid.SetColumn(valueHeader, 1);
            grid.Children.Add(valueHeader);

            var keyPair = DebuggerUtils.GetKeyFromString(key);

            if (keyPair != null) {

                var scriptHash = DebuggerUtils.AddressToScriptHash(keyPair.address);

                RenderLine(grid, 1, "Address", keyPair.address);
                RenderLine(grid, 2, "Script Hash (RAW, hex)", DebuggerUtils.ToHexString(scriptHash));
                RenderLine(grid, 3, "Script Hash (RAW, bytes)", DebuggerUtils.ToReadableByteArrayString(scriptHash));
                RenderLine(grid, 4, "Public Key (RAW, hex)", DebuggerUtils.ToHexString(keyPair.PublicKey));
                RenderLine(grid, 5, "Private Key (RAW, hex)", DebuggerUtils.ToHexString(keyPair.PrivateKey));
                RenderLine(grid, 6, "Private Key (WIF, hex)", keyPair.WIF);
                RenderLine(grid, 7, "Private Key (RAW, bytes)", DebuggerUtils.ToReadableByteArrayString(keyPair.PrivateKey));

            }
        }

        private void RenderLine(Grid grid, int rowCount, string v1, string v2)
        {
            var rowView = new RowDefinition { Height = new GridLength(25) };
            grid.RowDefinitions.Add(rowView);

            var v1View = new TextBlock {
                Text = v1,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(v1View, rowCount);
            Grid.SetColumn(v1View, 0);
            grid.Children.Add(v1View);

            var v2View = new TextBlock {
                Text = v2,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(v2View, rowCount);
            Grid.SetColumn(v2View, 1);
            grid.Children.Add(v2View);
        }
    }
}