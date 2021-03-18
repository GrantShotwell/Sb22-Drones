using System;
using System.Collections.Generic;
using System.Text;

namespace Sb22.Library {

	public class RollingList<T> : IEnumerable<T> {

		private T?[] Array { get; }
		private int Offset { get; }


		public RollingList(int size) {
			Array = new T[size];
			Offset = 0;
		}


		/// <summary>
		/// Inserts <paramref name="item"/> into the array.
		/// </summary>
		/// <param name="item"></param>
		/// <returns>Returns the <typeparamref name="T"/> removed.</returns>
		public T? Insert(T item) {
			Offset++;
			if(Offset >= Count) Offset = 0;
			T? removed = Array[Offset];
			Array[Offset] = item;
			return removed;
		}

		public IEnumerator<T> GetEnumerator() => new Enumerator(Array);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() as IEnumerable;

		public class Enumerator : IEnumerator<T> {

			private T?[] Array { get; }
			private int Index { get; set; }
			public T Current => Array[Index];


			public Enumerator(T?[] array) {
				Array = array.Clone() as T[];
				Reset();
			}


			public void Reset() {
				Index = -1;
			}

			public bool MoveNext() {
				if(++Index >= Array.Length) {
					Current = null;
					return false;
				} else {
					Current = Array[Index];
					if(Current == null) MoveNext();
					return true;
				}
			}

			public void Dispose() {
				// Nothing to dispose of.
			}

		}

	}

}
