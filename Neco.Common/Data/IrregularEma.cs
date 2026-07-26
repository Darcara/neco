namespace Neco.Common.Data;

/// <summary>
/// Calculates an exponential moving average, based on irregular period lengths
/// </summary>
/// <seealso href="https://oroboro.com/irregular-ema/"/>
/// <seealso href="https://stackoverflow.com/questions/56956832/fast-ema-calculation-on-large-dataset-with-irregular-time-intervals"/>
/// <seealso href="https://stackoverflow.com/questions/1023860/exponential-moving-average-sampled-at-varying-times"/>
public class IrregularEma {
	public static Double Next(Double sample, Double prevSample, Double emaPrev, Double alpha, Double deltaTime) {
		Double a = deltaTime / (1-alpha); 
		Double u = Math.Exp( a * -1 );
		Double v = ( 1 - u ) / a;
 
		Double emaNext = ( u * emaPrev ) + (( v - u ) * prevSample ) + (( 1.0 - v ) * sample );
		return emaNext;
	}

	// From https://github.com/stakewithus/notes/blob/main/notebook/ema.ipynb
	// From https://www.youtube.com/watch?v=22pT7rGbEv8
	public static Double Next2(Double p, Double u, Double dt) {
		Double H = 4;
		Double a_dt = 1 - Math.Exp(Math.Log(0.5) / H * dt);
		return a_dt * p + (1 - a_dt) * u;
	}
}