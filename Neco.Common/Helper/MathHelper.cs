namespace Neco.Common.Helper;

using System.Numerics;

public static class MathHelper {
	/// <summary>
	/// Does 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <param name="halfCircle"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	/// <remarks>Does not retain sign / direction</remarks>
	public static T CircularDistance<T>(T left, T right, T halfCircle) where T :INumberBase<T>, IComparisonOperators<T, T, Boolean> {
		return halfCircle - T.Abs(T.Abs(left - right) - halfCircle);
	}
}