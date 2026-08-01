using System.Collections.ObjectModel;
using ActionOrbit.App.Models;

namespace ActionOrbit.App.ViewModels;

public sealed class ShelfBoardViewModel : ViewModelBase
{
    public ShelfBoardViewModel(ShelfBoard board)
    {
        Board = board;
        Items = new ObservableCollection<ShelfItemViewModel>(board.Items.Select(item => new ShelfItemViewModel(item)));
    }

    public ShelfBoard Board { get; }
    public ObservableCollection<ShelfItemViewModel> Items { get; }
    public string Id => Board.Id;

    public string Name
    {
        get => Board.Name;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "Yeni Raf" : value.Trim();
            if (Board.Name == normalized)
            {
                return;
            }

            Board.Name = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public bool IsPinned
    {
        get => Board.IsPinned;
        set
        {
            if (Board.IsPinned == value)
            {
                return;
            }

            Board.IsPinned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string DisplayName => IsPinned ? $"📌 {Name}" : Name;
    public int ItemCount => Items.Count;
    public long TotalBytes => Items.Sum(item => item.Item.SizeBytes);

    public void Add(ShelfItem item)
    {
        Board.Items.Add(item);
        Items.Add(new ShelfItemViewModel(item));
        Touch();
    }

    public void Remove(ShelfItemViewModel item)
    {
        Board.Items.Remove(item.Item);
        Items.Remove(item);
        Touch();
    }

    public void Touch()
    {
        Board.LastUsedUtc = DateTime.UtcNow;
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(TotalBytes));
    }
}
