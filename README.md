# C# support for lightweight simple binary format.
C# support for using generated user types from [simple binary format](https://github.com/Leopotam/simplebinary.git).

# Example
Scheme of user types:
```json
{
    "Item": {
        "id": "u32",
        "count": "u32"
    },
    "Inventory": {
        "items": "Item[]"
    }
}
```
C# serialize / deserialize code:
```csharp
using System;
using Leopotam.SimpleBinary;

public class InventoryTest {
    public void Test () {
        var inv = Inventory.New ();
        var item = Item.New ();
        item.Id = 1;
        item.Count = 2;
        inv.Items.Add (item);
        // serialize.
        var buf = new byte[1024];
        var sbs = new SimpleBinarySerializer (buf);
        inv.Serialize (ref sbs);
        inv.Recycle();
        ArraySegment<byte> serializedData = sbs.GetBuffer ();
        // deserialize.
        var sbs2 = new SimpleBinarySerializer (serializedData.Array);
        if (sbs2.PeekPacketType () != Inventory.SB_PacketId) {
            throw new Exception ("invalid type");
        }
        var inv2 = Inventory.Deserialize (ref sbs2);
        // inv2.Items[0].id == 1
        // inv2.Items[0].count == 2
        inv2.Recycle();
    }
}
```