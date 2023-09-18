using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces.Workspaces
{
    public interface ITemplate<T> where T : class
    {
        public void Load(T template);

        public T Save();
    }
}
