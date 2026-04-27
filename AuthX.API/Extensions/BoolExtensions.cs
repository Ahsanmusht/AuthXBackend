namespace AuthX.Core.Extensions;

public static class FuncExtensions
{
    public static T Let<T>(this T self, Action<T> action)
    {
        action(self);
        return self;
    }
}