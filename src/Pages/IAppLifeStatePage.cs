using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.Pages
{
    public interface IAppLifeStatePage
    {
        public void OnBackground();
        public void OnResume();
    }
}
