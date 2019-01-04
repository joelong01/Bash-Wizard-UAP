using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace bashWizardShared
{
    public static class Extensions
    {
        /// <summary>
        ///     ObjservableCollection<> doesn't have an AddRange() API which we use.  Here it is an extension method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oc"></param>
        /// <param name="collection"></param>
        public static void AddRange<T>(this ObservableCollection<T> oc, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            foreach (T item in collection)
            {
                oc.Add(item);
            }


        }
    }
}
