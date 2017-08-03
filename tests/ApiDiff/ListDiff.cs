using System;
using System.Collections.Generic;

namespace Praeclarum
{
    /// <summary>
    /// The type of <see cref="ListDiffAction{S,D}"/>.
    /// </summary>
    public enum ListDiffActionType
    {
        /// <summary>
        /// Update the SourceItem to make it like the DestinationItem
        /// </summary>
        Update,
        /// <summary>
        /// Add the DestinationItem
        /// </summary>
        Add,
        /// <summary>
        /// Remove the SourceItem
        /// </summary>
        Remove,
    }
    
    /// <summary>
    /// A <see cref="ListDiff{S,D}"/> action that can be one of: Update, Add, or Remove.
    /// </summary>
    /// <typeparam name="S">The type of the source list elements</typeparam>
    /// <typeparam name="D">The type of the destination list elements</typeparam>
    public class ListDiffAction<S, D>
    {
        public ListDiffActionType ActionType;
        public S SourceItem;
        public D DestinationItem;

        public ListDiffAction(ListDiffActionType type, S source, D dest)
        {
            ActionType = type;
            SourceItem = source;
            DestinationItem = dest;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", ActionType, SourceItem, DestinationItem);
        }
    }

    /// <summary>
    /// Finds a diff between two lists (that contain possibly different types).
    /// <see cref="Actions"/> are generated such that the order of items in the
    /// destination list is preserved.
    /// The algorithm is from: http://en.wikipedia.org/wiki/Longest_common_subsequence_problem
    /// </summary>
    /// <typeparam name="S">The type of the source list elements</typeparam>
    /// <typeparam name="D">The type of the destination list elements</typeparam>
    public class ListDiff<S, D>
    {
        /// <summary>
        /// The actions needed to transform a source list to a destination list.
        /// </summary>
        public List<ListDiffAction<S, D>> Actions { get; private set; }

        /// <summary>
        /// Whether the <see cref="Actions"/> only contain Update actions
        /// (no Adds or Removes).
        /// </summary>
        public bool ContainsOnlyUpdates { get; private set; }

		public ListDiff(IEnumerable<S> sources, IEnumerable<D> destinations)
			: this (sources, destinations, (a,b) => a.Equals (b))
		{
		}

        public ListDiff(IEnumerable<S> sources,
                        IEnumerable<D> destinations,
                        Func<S, D, bool> match)
        {
            if (sources == null) throw new ArgumentNullException("sources");
            if (destinations == null) throw new ArgumentNullException("destinations");
            if (match == null) throw new ArgumentNullException("match");

            var x = new List<S>(sources);
            var y = new List<D>(destinations);

            Actions = new List<ListDiffAction<S, D>>();

            var m = x.Count;
            var n = y.Count;

            //
            // Construct the C matrix
            //
            var c = new int[m + 1, n + 1];
            for (var i = 1; i <= m; i++)
            {
                for (var j = 1; j <= n; j++)
                {
                    if (match(x[i - 1], y[j - 1]))
                    {
                        c[i, j] = c[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        c[i, j] = Math.Max(c[i, j - 1], c[i - 1, j]);
                    }
                }
            }

            //
            // Generate the actions
            //
            ContainsOnlyUpdates = true;
            GenDiff(c, x, y, m, n, match);
        }

        void GenDiff(int[,] c, List<S> x, List<D> y, int i, int j, Func<S, D, bool> match)
        {
            if (i > 0 && j > 0 && match(x[i - 1], y[j - 1]))
            {
                GenDiff(c, x, y, i - 1, j - 1, match);
                Actions.Add(new ListDiffAction<S, D>(ListDiffActionType.Update, x[i - 1], y[j - 1]));
            }
            else
            {
                if (j > 0 && (i == 0 || c[i, j - 1] >= c[i - 1, j]))
                {
                    GenDiff(c, x, y, i, j - 1, match);
                    ContainsOnlyUpdates = false;
                    Actions.Add(new ListDiffAction<S, D>(ListDiffActionType.Add, default(S), y[j - 1]));
                }
                else if (i > 0 && (j == 0 || c[i, j - 1] < c[i - 1, j]))
                {
                    GenDiff(c, x, y, i - 1, j, match);
                    ContainsOnlyUpdates = false;
                    Actions.Add(new ListDiffAction<S, D>(ListDiffActionType.Remove, x[i - 1], default(D)));
                }
            }
        }
    }

	public static class ListDiffEx
	{
		public static ListDiff<T, T> MergeInto<T> (this IList<T> source, IEnumerable<T> destination, Func<T, T, bool> match)
		{
			var diff = new ListDiff<T, T> (source, destination, match);

			var p = 0;

			foreach (var a in diff.Actions) {
				if (a.ActionType == ListDiffActionType.Add) {
					source.Insert (p, a.DestinationItem);
					p++;
				} else if (a.ActionType == ListDiffActionType.Remove) {
					source.RemoveAt (p);
				} else {
					p++;
				}
			}

			return diff;
		}

		public static ListDiff<TSource, TDestination> MergeInto<TSource, TDestination> (this IList<TSource> source, IEnumerable<TDestination> destination, Func<TSource, TDestination, bool> match, Func<TDestination, TSource> create, Action<TSource, TDestination> update, Action<TSource> delete)
		{
			var diff = new ListDiff<TSource, TDestination> (source, destination, match);

			var p = 0;

			foreach (var a in diff.Actions) {
				if (a.ActionType == ListDiffActionType.Add) {
					source.Insert (p, create (a.DestinationItem));
					p++;
				} else if (a.ActionType == ListDiffActionType.Remove) {
					delete (a.SourceItem);
					source.RemoveAt (p);
				} else {
					update (a.SourceItem, a.DestinationItem);
					p++;
				}
			}

			return diff;
		}

		public static ListDiff<TSource, TDestination> Diff<TSource, TDestination> (this IEnumerable<TSource> sources, IEnumerable<TDestination> destinations)
		{
			return new ListDiff<TSource, TDestination> (sources, destinations);
		}

		public static ListDiff<TSource, TDestination> Diff<TSource, TDestination> (this IEnumerable<TSource> sources, IEnumerable<TDestination> destinations, Func<TSource, TDestination, bool> match)
		{
			return new ListDiff<TSource, TDestination> (sources, destinations, match);
		}
	}
}
