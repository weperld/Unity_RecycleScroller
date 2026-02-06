# RecycleScroller Unity Package

High-performance cell recycling scroll view for Unity UI with loop scrolling, paging, and flexible grouping.

## Features

- **Object Pooling**: Efficient cell recycling system
- **Dual Axis Support**: Vertical and Horizontal scrolling
- **Loop Scrolling**: Seamless infinite scroll
- **Pagination**: Page-based navigation with snapping
- **Flexible Grouping**: Dynamic cell grouping (rows/columns)
- **Async Data Loading**: Synchronous and asynchronous data loading
- **Easing Functions**: 32+ built-in easing functions
- **Extensible**: Delegate pattern for customization

## Installation

### Via Git URL

1. Open Unity Package Manager (Window > Package Manager)
2. Click the "+" button
3. Select "Add package from git URL"
4. Enter: `https://github.com/weperld/Unity_RecycleScroller.git`

### Via Local Folder

1. Copy `RecycleScrollerPackage` folder to your Unity project's `Packages` folder
2. Restart Unity Editor

## Requirements

- Unity 2022.3 or later
- com.cysharp.unitask 2.5.0 or later

## Quick Start

```csharp
using UnityEngine;
using RecycleScroll;

public class MyScrollController : MonoBehaviour
{
    [SerializeField] private RecycleScroller scroller;
    [SerializeField] private RecycleScrollerCell cellPrefab;
    
    private MyData[] dataList;
    
    private void Start()
    {
        // Initialize data
        dataList = new MyData[100];
        for (int i = 0; i < dataList.Length; i++)
        {
            dataList[i] = new MyData { id = i, name = $"Item {i}" };
        }
        
        // Set delegate
        scroller.del = new MyScrollDelegate();
        
        // Load data
        scroller.LoadData();
    }
    
    public class MyScrollDelegate : IRecycleScrollerDelegate
    {
        public RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex)
        {
            // Get prefab (you can use different prefabs based on data type)
            var prefab = Resources.Load<RecycleScrollerCell>("MyCell");
            var cell = scroller.GetCellInstance(prefab, dataIndex);
            
            // Customize cell based on data
            cell.name = $"Cell_{dataIndex}";
            
            return cell;
        }
        
        public int GetCellCount(RecycleScroller scroller) => dataList.Length;
        
        public RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex)
        {
            return new RSCellRect(100f, 200f, scroller);
        }
    }
    
    public class MyData
    {
        public int id;
        public string name;
    }
}
```

## API Reference

### Core Methods

| Method | Description |
|--------|-------------|
| `LoadData(params LoadParam[] _params)` | Load data synchronously |
| `LoadDataAsync(params LoadParam[] _params)` | Load data asynchronously |
| `MoveToIndex(int index, Ease ease, float duration)` | Scroll to specific cell index |
| `MoveToPage(int pageIndex)` | Scroll to specific page |
| `Insert(int index, int count)` | Insert cells at index |
| `Remove(int index, int count)` | Remove cells from index |

### Delegate Interface

Implement `IRecycleScrollerDelegate` to customize cell behavior:

```csharp
public interface IRecycleScrollerDelegate
{
    RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex);
    int GetCellCount(RecycleScroller scroller);
    RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex);
}
```

## Inspector Settings

### Scroll Settings
- **Scroll Axis**: Vertical / Horizontal
- **Loop Scroll**: Enable infinite scrolling
- **Reverse**: Reverse scroll direction
- **Padding**: Content padding
- **Spacing**: Spacing between cells
- **Child Alignment**: Alignment for child elements

### Paging Settings
- **Use Paging**: Enable pagination
- **Count Per Page**: Number of cells per page
- **Duration**: Animation duration
- **Easing**: Easing function for page transitions

### Cell Settings
- **Fixed Cell Count**: Fixed number of cells per group
- **Flexible Cell Count**: Min/max cells per group
- **Use One Cell Rect**: Use same rect for all cells

## Samples

### Basic Example
Basic RecycleScroller usage with simple list items.

### Group Example
RecycleScroller with grouping support.

## License

See LICENSE file for details.

## Support

For issues and questions, please visit:
- GitHub: https://github.com/weperld/Unity_RecycleScroller
- Email: contact@tikitaka-studio.com
