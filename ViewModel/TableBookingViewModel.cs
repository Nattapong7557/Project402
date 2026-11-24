namespace Nattapong_7379_L002.ViewModel;

public sealed class TableBookingViewModel : BookingFormViewModel
{
    public string MinimumDate { get; set; } = string.Empty;
    public IReadOnlyList<string> Unavailable { get; set; } = [];
}
