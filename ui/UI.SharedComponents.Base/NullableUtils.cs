using Microsoft.AspNetCore.Components;

namespace UI.SharedComponents.Base;

public class NullableUtils
{
    public static int? AsNullableInt<T>(T value)
    {
        if (value is null)
            return null;

        return (int)(object)value;
    }

    public static int AsInt<T>(T value)
    {
        return value is null ? 0 : (int)(object)value;
    }
    
    public static Task OnInnerValueChanged<T>(int? value, bool isRequired, EventCallback<T> valueChanged)
    {
        object? result;
        if (Nullable.GetUnderlyingType(typeof(T)) is not null)
        {
            result = value;
        }
        else
        {
            if (isRequired && value is null) return Task.CompletedTask;
            result = value ?? default(int);
        }
        return valueChanged.InvokeAsync((T)result!);
    }
    
    public static Task OnInnerValueChanged<T>(int value, bool isRequired, EventCallback<T> valueChanged)
    {
        object? result;
        if (isRequired && value == 0) return Task.CompletedTask;
        result = value == 0 ? null : value;
        return valueChanged.InvokeAsync((T)result!);
    }
}