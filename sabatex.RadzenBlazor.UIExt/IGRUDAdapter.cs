using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sabatex.RadzenBlazor;

public interface IGRUDAdapter
{
    T Get<T>() where T : class;
    T Post<T>(T item) where T : class;
    T Update<T>(T item) where T : class;
    T Delete<T>(T item) where T : class;
}
