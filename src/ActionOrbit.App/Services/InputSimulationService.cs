using System.Runtime.InteropServices;
using System.ComponentModel;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Services;

public sealed class InputSimulationService
{
    private readonly LogService _logService;

    public InputSimulationService(LogService logService)
    {
        _logService = logService;
    }

    public Task SendHotkeyAsync(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return Task.CompletedTask;
        }

        if (!HotkeyChordParser.TryParseTokens(hotkey, out var tokens))
        {
            throw new InvalidOperationException($"Could not parse hotkey: {hotkey}");
        }

        var keyToken = tokens[^1];
        var modifierTokens = tokens.Take(tokens.Count - 1).ToList();

        if (!KeyTokenParser.TryParseVirtualKey(keyToken, out var mainKey))
        {
            throw new InvalidOperationException($"Could not parse hotkey key: {keyToken}");
        }

        var modifiers = new List<uint>();
        foreach (var modifierToken in modifierTokens)
        {
            if (!KeyTokenParser.TryParseVirtualKey(modifierToken, out var modifierKey))
            {
                throw new InvalidOperationException($"Could not parse hotkey modifier: {modifierToken}");
            }

            modifiers.Add(modifierKey);
        }

        var inputs = new List<NativeMethods.Input>();
        inputs.AddRange(modifiers.Select(modifier => KeyInput(modifier, keyUp: false)));
        inputs.Add(KeyInput(mainKey, keyUp: false));
        inputs.Add(KeyInput(mainKey, keyUp: true));

        for (var i = modifiers.Count - 1; i >= 0; i--)
        {
            inputs.Add(KeyInput(modifiers[i], keyUp: true));
        }

        Send(inputs);
        return Task.CompletedTask;
    }

    public Task TypeTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Task.CompletedTask;
        }

        var inputs = new List<NativeMethods.Input>();
        foreach (var character in text)
        {
            inputs.Add(UnicodeInput(character, keyUp: false));
            inputs.Add(UnicodeInput(character, keyUp: true));
        }

        Send(inputs);
        return Task.CompletedTask;
    }

    private void Send(IReadOnlyList<NativeMethods.Input> inputs)
    {
        if (inputs.Count == 0)
        {
            return;
        }

        var array = inputs.ToArray();
        var sent = NativeMethods.SendInput((uint)array.Length, array, Marshal.SizeOf<NativeMethods.Input>());
        try
        {
            EnsureAllInputsWereSent(array.Length, sent, Marshal.GetLastWin32Error());
        }
        catch (Win32Exception ex)
        {
            _logService.Warn(
                $"SendInput sent {sent} of {array.Length} inputs. LastError={ex.NativeErrorCode} ({ex.Message}).");
            throw;
        }
    }

    internal static void EnsureAllInputsWereSent(int expected, uint sent, int error)
    {
        if (sent == expected)
        {
            return;
        }

        var message = error == 0
            ? "no Win32 error"
            : new Win32Exception(error).Message;
        throw new Win32Exception(
            error,
            $"Windows girdinin tamamını gönderemedi ({sent}/{expected}). {message}");
    }

    private static NativeMethods.Input KeyInput(uint virtualKey, bool keyUp) =>
        new()
        {
            Type = NativeMethods.InputKeyboard,
            Data = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KeyboardInput
                {
                    VirtualKey = (ushort)virtualKey,
                    ScanCode = 0,
                    Flags = keyUp ? NativeMethods.KeyEventFKeyUp : 0
                }
            }
        };

    private static NativeMethods.Input UnicodeInput(char character, bool keyUp) =>
        new()
        {
            Type = NativeMethods.InputKeyboard,
            Data = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KeyboardInput
                {
                    VirtualKey = 0,
                    ScanCode = character,
                    Flags = NativeMethods.KeyEventFUnicode | (keyUp ? NativeMethods.KeyEventFKeyUp : 0)
                }
            }
        };
}
