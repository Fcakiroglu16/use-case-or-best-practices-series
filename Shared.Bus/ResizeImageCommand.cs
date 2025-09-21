namespace Shared.Bus
{
    public record ResizeImageCommand(string ImageUrl, int Width, int Height);
}