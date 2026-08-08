using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Views.MiniTools;

public partial class CalculatorToolView : System.Windows.Controls.UserControl
{
    private string? _result;

    public CalculatorToolView()
    {
        InitializeComponent();
        Loaded += (_, _) => ExpressionBox.Focus();
    }

    private void Token_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string token })
        {
            return;
        }

        var start = ExpressionBox.SelectionStart;
        ExpressionBox.Text = ExpressionBox.Text.Insert(start, token);
        ExpressionBox.SelectionStart = start + token.Length;
        ExpressionBox.Focus();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        ExpressionBox.Clear();
        _result = null;
        ResultText.Text = "—";
        ResultLabel.Text = "Sonuç";
        ExpressionBox.Focus();
    }

    private void Backspace_Click(object sender, RoutedEventArgs e)
    {
        var start = ExpressionBox.SelectionStart;
        var length = ExpressionBox.SelectionLength;
        if (length > 0)
        {
            ExpressionBox.Text = ExpressionBox.Text.Remove(start, length);
            ExpressionBox.SelectionStart = start;
        }
        else if (start > 0)
        {
            ExpressionBox.Text = ExpressionBox.Text.Remove(start - 1, 1);
            ExpressionBox.SelectionStart = start - 1;
        }

        ExpressionBox.Focus();
    }

    private void Evaluate_Click(object sender, RoutedEventArgs e) => Evaluate();

    private void ExpressionBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Return)
        {
            Evaluate();
            e.Handled = true;
        }
    }

    private void Evaluate()
    {
        if (CalculatorEngine.TryEvaluate(ExpressionBox.Text, out var value, out var issue))
        {
            _result = CalculatorEngine.Format(value);
            ResultText.Text = _result;
            ResultLabel.Text = "Sonuç";
        }
        else
        {
            _result = null;
            ResultText.Text = "Hata";
            ResultLabel.Text = issue;
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_result))
        {
            ResultLabel.Text = "Önce geçerli bir sonuç hesapla";
            return;
        }

        try
        {
            System.Windows.Clipboard.SetText(_result);
            ResultLabel.Text = "Panoya kopyalandı";
        }
        catch (Exception)
        {
            ResultLabel.Text = "Pano şu anda kullanılıyor";
        }
    }
}
