namespace Neco.Test.Common;

using System;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Neco.Common;
using NUnit.Framework;

[TestFixture]
public class CrontabTests {
	private static readonly CultureInfo _testCulture=CultureInfo.GetCultureInfoByIetfLanguageTag("en-gb");
			
	[Test]
	[TestCase("* * * *")]
	[TestCase("* * * * * * *")]
	public void InvalidArguments(String expression)
	{
		Assert.Throws<ArgumentException>(() => Crontab.Parse(expression));
		Assert.That(Crontab.TryParse(expression, out Crontab _), Is.False);
	}

	[TestCase("")]
	[TestCase(null!)]
	public void InvalidNullArguments(String expression)
	{
		Assert.Throws<ArgumentNullException>(() => Crontab.Parse(expression));
		Assert.That(Crontab.TryParse(expression, out Crontab _), Is.False);
	}
		
	[TestCase("@notAnAlias")]
	public void InvalidAlias(String expression)
	{
		Assert.Throws<ArgumentException>(() => Crontab.Parse(expression));
		Assert.That(Crontab.TryParse(expression, out Crontab _), Is.False);
	}

	[TestCase("-1 * * * * *")]
	[TestCase("61 * * * * *")]
	[TestCase("* 61 * * * *")]
	[TestCase("* * 24 * * *")]
	[TestCase("* * * 0 * *")]
	[TestCase("* * * 32 * *")]
	[TestCase("* * * * 0 *")]
	[TestCase("* * * * 13 *")]
	[TestCase("* * * * * 7")]
	[TestCase("JAN * * * * *")]
	[TestCase("* JAN * * * *")]
	[TestCase("* * JAN * * *")]
	[TestCase("* * * JAN * *")]
	[TestCase("* * * * SUN *")]
	[TestCase("* * * * * JAN")]
	[TestCase("SUN * * * * *")]
	[TestCase("* SUN * * * *")]
	[TestCase("* * SUN * * *")]
	[TestCase("* * * SUN * *")]
	[TestCase("1-/7 * * * * *")]
	[TestCase("/7 * * * * *")]
	[TestCase("1-10/ * * * * *")]
	[TestCase("1-10/0 * * * * *")]
	[TestCase("1-10/61 * * * * *")]
	[TestCase("1-10-20 * * * * *")]
	[TestCase("1- * * * * *")]
	[TestCase("10-1 * * * * *")]
	[TestCase("10-10 * * * * *")]
	public void InvalidComponents(String expression)
	{
		Console.WriteLine(Assert.Throws<ValueParseException>(() => Crontab.Parse(expression)));
		Assert.That(Crontab.TryParse(expression, out Crontab _), Is.False);
	}

	[Test]
	[TestCase("* * * * *")]
	[TestCase("* * * * * *")]
	[TestCase("00 00 * * * *")]
	[TestCase("*   00   *    *    *    *")]
	public void Valid(String expression)
	{
		Crontab.Parse(expression);
		Assert.That(Crontab.TryParse(expression, out Crontab _), Is.True);
	}

	[Test]
	[TestCase("* * * * *", "2017-01-16 01:01:01", "2017-01-16 01:02:00")]
	[TestCase("* * * * * *", "2017-01-16 01:01:01", "2017-01-16 01:01:02")]
	public void SimpleTestCases(String expression, String dateTime, String nextDateTime)
	{
		Crontab cron = Crontab.Parse(expression);
		cron.PointInTime = DateTime.Parse(dateTime, _testCulture, DateTimeStyles.AssumeLocal);
		DateTime next = cron.NextPointInTime;
		Console.WriteLine(cron.ToDebugString());
		Assert.That(next, Is.EqualTo(DateTime.Parse(nextDateTime, _testCulture, DateTimeStyles.AssumeLocal)));

		cron.CalculateNextOccurrences().Take(25).ToList().ForEach(dt => Console.WriteLine(dt));
	}

	[TestCase("* 1-3 * * *", "* 1-2,3 * * *")]
	[TestCase("* * * 1,3,5,7,9,11 *", "* * * */2 *")]
	[TestCase("10,25,40 * * * *", "10-40/15 * * * *")]
	[TestCase("* * * 1,3,8 1-2,5", "* * * Mar,Jan,Aug Fri,Mon-Tue")]
	[TestCase("1 * 1-3 * * *", "1 * 1-2,3 * * *")]
	[TestCase("22 * * * 1,3,5,7,9,11 *", "22 * * * */2 *")]
	[TestCase("33 10,25,40 * * * *", "33 10-40/15 * * * *")]
	[TestCase("55 * * * 1,3,8 1-2,5", "55 * * * Mar,Jan,Aug Fri,Mon-Tue")]
	[TestCase("@yearly", "0 0 0 1 1 *")]
	[TestCase("@annually", "@yearly")]
	[TestCase("@monthly", "0 0 0 1 * *")]
	[TestCase("@weekly", "0 0 0 * * 0")]
	[TestCase("@daily", "0 0 0 * * *")]
	[TestCase("@midnight", "@daily")]
	[TestCase("@hourly", "0 0 * * * *")]
	public void Formatting(String exp1, String exp2)
	{
		Crontab cron1 = Crontab.Parse(exp1);
		Crontab cron2 = Crontab.Parse(exp2);
		cron2.PointInTime = cron1.PointInTime;
		Console.WriteLine(cron1.ToDebugString());
		Console.WriteLine(cron2.ToDebugString());
		Assert.That(cron1.ToDebugString(), Is.EqualTo(cron2.ToDebugString()));
		Assert.That(cron1.GetHashCode(), Is.EqualTo(cron2.GetHashCode()));
		Assert.That(cron1 == cron2, Is.True);
		Assert.That(cron1, Is.EqualTo(cron2));
		if (!exp1.StartsWith('@')) {
			Assert.That(cron1.Expression, Is.EqualTo(exp1));
			Assert.That(cron1.ToString(), Is.EqualTo(exp1));
			Assert.That(cron2.Expression, Is.EqualTo(exp2));
			Assert.That(cron1.Expression, Is.Not.EqualTo(cron2.Expression));
		}
	}

	[TestCase("01/01/2003 00:00:00", "* * * * *", "01/01/2003 00:01:00")]
	[TestCase("01/01/2003 00:01:00", "* * * * *", "01/01/2003 00:02:00")]
	[TestCase("01/01/2003 00:02:00", "* * * * *", "01/01/2003 00:03:00")]
	[TestCase("01/01/2003 00:59:00", "* * * * *", "01/01/2003 01:00:00")]
	[TestCase("01/01/2003 01:59:00", "* * * * *", "01/01/2003 02:00:00")]
	[TestCase("01/01/2003 23:59:00", "* * * * *", "02/01/2003 00:00:00")]
	[TestCase("31/12/2003 23:59:00", "* * * * *", "01/01/2004 00:00:00")]
	[TestCase("28/02/2003 23:59:00", "* * * * *", "01/03/2003 00:00:00")]
	[TestCase("28/02/2004 23:59:00", "* * * * *", "29/02/2004 00:00:00")]

	// Second tests
	[TestCase("01/01/2003 00:00:00", "45 * * * * *", "01/01/2003 00:00:45")]
	[TestCase("01/01/2003 00:00:00", "45-47,48,49 * * * * *", "01/01/2003 00:00:45")]
	[TestCase("01/01/2003 00:00:45", "45-47,48,49 * * * * *", "01/01/2003 00:00:46")]
	[TestCase("01/01/2003 00:00:46", "45-47,48,49 * * * * *", "01/01/2003 00:00:47")]
	[TestCase("01/01/2003 00:00:47", "45-47,48,49 * * * * *", "01/01/2003 00:00:48")]
	[TestCase("01/01/2003 00:00:48", "45-47,48,49 * * * * *", "01/01/2003 00:00:49")]
	[TestCase("01/01/2003 00:00:49", "45-47,48,49 * * * * *", "01/01/2003 00:01:45")]
	[TestCase("01/01/2003 00:00:00", "2/5 * * * * *", "01/01/2003 00:00:02")]
	[TestCase("01/01/2003 00:00:02", "2/5 * * * * *", "01/01/2003 00:00:07")]
	[TestCase("01/01/2003 00:00:50", "2/5 * * * * *", "01/01/2003 00:00:52")]
	[TestCase("01/01/2003 00:00:52", "2/5 * * * * *", "01/01/2003 00:00:57")]
	[TestCase("01/01/2003 00:00:57", "2/5 * * * * *", "01/01/2003 00:01:02")]

	// Minute tests
	[TestCase("01/01/2003 00:00:00", "45 * * * *", "01/01/2003 00:45:00")]
	[TestCase("01/01/2003 00:00:00", "45-47,48,49 * * * *", "01/01/2003 00:45:00")]
	[TestCase("01/01/2003 00:45:00", "45-47,48,49 * * * *", "01/01/2003 00:46:00")]
	[TestCase("01/01/2003 00:46:00", "45-47,48,49 * * * *", "01/01/2003 00:47:00")]
	[TestCase("01/01/2003 00:47:00", "45-47,48,49 * * * *", "01/01/2003 00:48:00")]
	[TestCase("01/01/2003 00:48:00", "45-47,48,49 * * * *", "01/01/2003 00:49:00")]
	[TestCase("01/01/2003 00:49:00", "45-47,48,49 * * * *", "01/01/2003 01:45:00")]
	[TestCase("01/01/2003 00:00:00", "2/5 * * * *", "01/01/2003 00:02:00")]
	[TestCase("01/01/2003 00:02:00", "2/5 * * * *", "01/01/2003 00:07:00")]
	[TestCase("01/01/2003 00:50:00", "2/5 * * * *", "01/01/2003 00:52:00")]
	[TestCase("01/01/2003 00:52:00", "2/5 * * * *", "01/01/2003 00:57:00")]
	[TestCase("01/01/2003 00:57:00", "2/5 * * * *", "01/01/2003 01:02:00")]
	[TestCase("01/01/2003 00:00:30", "3 45 * * * *", "01/01/2003 00:45:03")]
	[TestCase("01/01/2003 00:00:30", "6 45-47,48,49 * * * *", "01/01/2003 00:45:06")]
	[TestCase("01/01/2003 00:45:30", "6 45-47,48,49 * * * *", "01/01/2003 00:46:06")]
	[TestCase("01/01/2003 00:46:30", "6 45-47,48,49 * * * *", "01/01/2003 00:47:06")]
	[TestCase("01/01/2003 00:47:30", "6 45-47,48,49 * * * *", "01/01/2003 00:48:06")]
	[TestCase("01/01/2003 00:48:30", "6 45-47,48,49 * * * *", "01/01/2003 00:49:06")]
	[TestCase("01/01/2003 00:49:30", "6 45-47,48,49 * * * *", "01/01/2003 01:45:06")]
	[TestCase("01/01/2003 00:00:30", "9 2/5 * * * *", "01/01/2003 00:02:09")]
	[TestCase("01/01/2003 00:02:30", "9 2/5 * * * *", "01/01/2003 00:07:09")]
	[TestCase("01/01/2003 00:50:30", "9 2/5 * * * *", "01/01/2003 00:52:09")]
	[TestCase("01/01/2003 00:52:30", "9 2/5 * * * *", "01/01/2003 00:57:09")]
	[TestCase("01/01/2003 00:57:30", "9 2/5 * * * *", "01/01/2003 01:02:09")]

	// Hour tests
	[TestCase("20/12/2003 10:00:00", " * 3/4 * * *", "20/12/2003 11:00:00")]
	[TestCase("20/12/2003 00:30:00", " * 3   * * *", "20/12/2003 03:00:00")]
	[TestCase("20/12/2003 01:45:00", "30 3   * * *", "20/12/2003 03:30:00")]

	// Day of month tests
	[TestCase("07/01/2003 00:00:00", "30  *  1 * *", "01/02/2003 00:30:00")]
	[TestCase("01/02/2003 00:30:00", "30  *  1 * *", "01/02/2003 01:30:00")]
	[TestCase("01/01/2003 00:00:00", "10  * 22    * *", "22/01/2003 00:10:00")]
	[TestCase("01/01/2003 00:00:00", "30 23 19    * *", "19/01/2003 23:30:00")]
	[TestCase("01/01/2003 00:00:00", "30 23 21    * *", "21/01/2003 23:30:00")]
	[TestCase("01/01/2003 00:01:00", " *  * 21    * *", "21/01/2003 00:00:00")]
	[TestCase("10/07/2003 00:00:00", " *  * 30,31 * *", "30/07/2003 00:00:00")]

	// Test month rollovers for months with 28,29,30 and 31 days
	[TestCase("28/02/2002 23:59:59", "* * * 3 *", "01/03/2002 00:00:00")]
	[TestCase("29/02/2004 23:59:59", "* * * 3 *", "01/03/2004 00:00:00")]
	[TestCase("31/03/2002 23:59:59", "* * * 4 *", "01/04/2002 00:00:00")]
	[TestCase("30/04/2002 23:59:59", "* * * 5 *", "01/05/2002 00:00:00")]

	// Test month 30,31 days
	[TestCase("01/01/2000 00:00:00", "0 0 15,30,31 * *", "15/01/2000 00:00:00")]
	[TestCase("15/01/2000 00:00:00", "0 0 15,30,31 * *", "30/01/2000 00:00:00")]
	[TestCase("30/01/2000 00:00:00", "0 0 15,30,31 * *", "31/01/2000 00:00:00")]
	[TestCase("31/01/2000 00:00:00", "0 0 15,30,31 * *", "15/02/2000 00:00:00")]
	[TestCase("15/02/2000 00:00:00", "0 0 15,30,31 * *", "15/03/2000 00:00:00")]
	[TestCase("15/03/2000 00:00:00", "0 0 15,30,31 * *", "30/03/2000 00:00:00")]
	[TestCase("30/03/2000 00:00:00", "0 0 15,30,31 * *", "31/03/2000 00:00:00")]
	[TestCase("31/03/2000 00:00:00", "0 0 15,30,31 * *", "15/04/2000 00:00:00")]
	[TestCase("15/04/2000 00:00:00", "0 0 15,30,31 * *", "30/04/2000 00:00:00")]
	[TestCase("30/04/2000 00:00:00", "0 0 15,30,31 * *", "15/05/2000 00:00:00")]
	[TestCase("15/05/2000 00:00:00", "0 0 15,30,31 * *", "30/05/2000 00:00:00")]
	[TestCase("30/05/2000 00:00:00", "0 0 15,30,31 * *", "31/05/2000 00:00:00")]
	[TestCase("31/05/2000 00:00:00", "0 0 15,30,31 * *", "15/06/2000 00:00:00")]
	[TestCase("15/06/2000 00:00:00", "0 0 15,30,31 * *", "30/06/2000 00:00:00")]
	[TestCase("30/06/2000 00:00:00", "0 0 15,30,31 * *", "15/07/2000 00:00:00")]
	[TestCase("15/07/2000 00:00:00", "0 0 15,30,31 * *", "30/07/2000 00:00:00")]
	[TestCase("30/07/2000 00:00:00", "0 0 15,30,31 * *", "31/07/2000 00:00:00")]
	[TestCase("31/07/2000 00:00:00", "0 0 15,30,31 * *", "15/08/2000 00:00:00")]
	[TestCase("15/08/2000 00:00:00", "0 0 15,30,31 * *", "30/08/2000 00:00:00")]
	[TestCase("30/08/2000 00:00:00", "0 0 15,30,31 * *", "31/08/2000 00:00:00")]
	[TestCase("31/08/2000 00:00:00", "0 0 15,30,31 * *", "15/09/2000 00:00:00")]
	[TestCase("15/09/2000 00:00:00", "0 0 15,30,31 * *", "30/09/2000 00:00:00")]
	[TestCase("30/09/2000 00:00:00", "0 0 15,30,31 * *", "15/10/2000 00:00:00")]
	[TestCase("15/10/2000 00:00:00", "0 0 15,30,31 * *", "30/10/2000 00:00:00")]
	[TestCase("30/10/2000 00:00:00", "0 0 15,30,31 * *", "31/10/2000 00:00:00")]
	[TestCase("31/10/2000 00:00:00", "0 0 15,30,31 * *", "15/11/2000 00:00:00")]
	[TestCase("15/11/2000 00:00:00", "0 0 15,30,31 * *", "30/11/2000 00:00:00")]
	[TestCase("30/11/2000 00:00:00", "0 0 15,30,31 * *", "15/12/2000 00:00:00")]
	[TestCase("15/12/2000 00:00:00", "0 0 15,30,31 * *", "30/12/2000 00:00:00")]
	[TestCase("30/12/2000 00:00:00", "0 0 15,30,31 * *", "31/12/2000 00:00:00")]
	[TestCase("31/12/2000 00:00:00", "0 0 15,30,31 * *", "15/01/2001 00:00:00")]

	// Other month tests (including year rollover)
	[TestCase("01/12/2003 05:00:00", "10 * * 6 *", "01/06/2004 00:10:00")]
	[TestCase("04/01/2003 00:00:00", " 1 2 3 * *", "03/02/2003 02:01:00")]
	[TestCase("01/07/2002 05:00:00", "10 * * February,April-Jun *", "01/02/2003 00:10:00")]
	[TestCase("01/01/2003 00:00:00", "0 12 1 6 *", "01/06/2003 12:00:00")]
	[TestCase("11/09/1988 14:23:00", "* 12 1 6 *", "01/06/1989 12:00:00")]
	[TestCase("11/03/1988 14:23:00", "* 12 1 6 *", "01/06/1988 12:00:00")]
	[TestCase("11/03/1988 14:23:00", "* 2,4-8,15 * 6 *", "01/06/1988 02:00:00")]
	[TestCase("11/03/1988 14:23:00", "20 * * january,FeB,Mar,april,May,JuNE,July,Augu,SEPT-October,Nov,DECEM *", "11/03/1988 15:20:00")]

	// Day of week tests
	[TestCase("26/06/2003 10:00:00", "30 6 * * 0", "29/06/2003 06:30:00")]
	[TestCase("26/06/2003 10:00:00", "30 6 * * sunday", "29/06/2003 06:30:00")]
	[TestCase("26/06/2003 10:00:00", "30 6 * * SUNDAY", "29/06/2003 06:30:00")]
	[TestCase("19/06/2003 00:00:00", "1 12 * * 2", "24/06/2003 12:01:00")]
	[TestCase("24/06/2003 12:01:00", "1 12 * * 2", "01/07/2003 12:01:00")]
	[TestCase("01/06/2003 14:55:00", "15 18 * * Mon", "02/06/2003 18:15:00")]
	[TestCase("02/06/2003 18:15:00", "15 18 * * Mon", "09/06/2003 18:15:00")]
	[TestCase("09/06/2003 18:15:00", "15 18 * * Mon", "16/06/2003 18:15:00")]
	[TestCase("16/06/2003 18:15:00", "15 18 * * Mon", "23/06/2003 18:15:00")]
	[TestCase("23/06/2003 18:15:00", "15 18 * * Mon", "30/06/2003 18:15:00")]
	[TestCase("30/06/2003 18:15:00", "15 18 * * Mon", "07/07/2003 18:15:00")]
	[TestCase("01/01/2003 00:00:00", "* * * * Mon", "06/01/2003 00:00:00")]
	[TestCase("01/01/2003 12:00:00", "45 16 1 * Mon", "01/09/2003 16:45:00")]
	[TestCase("01/09/2003 23:45:00", "45 16 1 * Mon", "01/12/2003 16:45:00")]

	// Leap year tests
	[TestCase("01/01/2000 12:00:00", "1 12 29 2 *", "29/02/2000 12:01:00")]
	[TestCase("29/02/2000 12:01:00", "1 12 29 2 *", "29/02/2004 12:01:00")]
	[TestCase("29/02/2004 12:01:00", "1 12 29 2 *", "29/02/2008 12:01:00")]

	// Non-leap year tests
	[TestCase("01/01/2000 12:00:00", "1 12 28 2 *", "28/02/2000 12:01:00")]
	[TestCase("28/02/2000 12:01:00", "1 12 28 2 *", "28/02/2001 12:01:00")]
	[TestCase("28/02/2001 12:01:00", "1 12 28 2 *", "28/02/2002 12:01:00")]
	[TestCase("28/02/2002 12:01:00", "1 12 28 2 *", "28/02/2003 12:01:00")]
	[TestCase("28/02/2003 12:01:00", "1 12 28 2 *", "28/02/2004 12:01:00")]
	[TestCase("29/02/2004 12:01:00", "1 12 28 2 *", "28/02/2005 12:01:00")]
	[TestCase("01/01/2000 12:00:00", "40 14/1 * * *", "01/01/2000 14:40:00")]
	[TestCase("01/01/2000 14:40:00", "40 14/1 * * *", "01/01/2000 15:40:00")]

	// End of time tests
	[TestCase("29/02/9988 00:00:00", "0 0 29 Feb Mon", "9999-12-31 23:59:59.999")]
	public void Evaluations(String startTimeString, String cronExpression, String nextTimeString)
	{
		Crontab cron = Crontab.Parse(cronExpression);
		DateTime nextTimeDirect = cron.CalculateNextOccurrence(DateTime.Parse(startTimeString, _testCulture, DateTimeStyles.AssumeLocal));
		cron.PointInTime = DateTime.Parse(startTimeString, _testCulture, DateTimeStyles.AssumeLocal);
		DateTime nextTime = cron.NextPointInTime;
		Console.WriteLine(cron.ToDebugString());
		Assert.That(nextTime, Is.EqualTo(nextTimeDirect));
		Assert.That(nextTime, Is.EqualTo(DateTime.Parse(nextTimeString, _testCulture, DateTimeStyles.AssumeLocal)).Within(TimeSpan.FromMilliseconds(1)));

		cron.CalculateNextOccurrences().Take(25).ToList().ForEach(dt => Console.WriteLine(dt));
	}

	[TestCase(" *  * * * *  ", "01/01/2003 00:00:00", "01/01/2003 00:00:00")]
	[TestCase(" *  * * * *  ", "31/12/2002 23:59:59", "01/01/2003 00:00:00")]
	[TestCase(" *  * * * Mon", "31/12/2002 23:59:59", "01/01/2003 00:00:00")]
	[TestCase(" *  * * * Mon", "01/01/2003 00:00:00", "02/01/2003 00:00:00")]
	[TestCase(" *  * * * Mon", "01/01/2003 00:00:00", "02/01/2003 12:00:00")]
	[TestCase("30 12 * * Mon", "01/01/2003 00:00:00", "06/01/2003 12:00:00")]
	[TestCase(" *  *  * * * *  ", "01/01/2003 00:00:00", "01/01/2003 00:00:00")]
	[TestCase(" *  *  * * * *  ", "31/12/2002 23:59:59", "01/01/2003 00:00:00")]
	[TestCase(" *  *  * * * Mon", "31/12/2002 23:59:59", "01/01/2003 00:00:00")]
	[TestCase(" *  *  * * * Mon", "01/01/2003 00:00:00", "02/01/2003 00:00:00")]
	[TestCase(" *  *  * * * Mon", "01/01/2003 00:00:00", "02/01/2003 12:00:00")]
	[TestCase("10 30 12 * * Mon", "01/01/2003 00:00:00", "06/01/2003 12:00:10")]
	public void NeverOccurs(String cronExpression, String startTimeString, String endTimeString)
	{
		Crontab cron = Crontab.Parse(cronExpression);
		DateTime nextTimeDirect = cron.CalculateNextOccurrence(DateTime.Parse(startTimeString, _testCulture, DateTimeStyles.AssumeLocal), DateTime.Parse(endTimeString, _testCulture, DateTimeStyles.AssumeLocal));

		Assert.That(nextTimeDirect, Is.EqualTo(DateTime.MaxValue).Within(TimeSpan.FromMilliseconds(1)));

		cron.PointInTime = DateTime.Parse(startTimeString, _testCulture, DateTimeStyles.AssumeLocal);
		cron.CalculateNextOccurrences().Take(25).ToList().ForEach(dt => Console.WriteLine(dt));
	}

	[TestCase("* * 31 Feb *")]
	[TestCase("* * * 31 Feb *")]
	public void DontLoopIndefinitely(String expression)
	{
		Crontab cron = Crontab.Parse(expression);
		Assert.That(cron.NextPointInTime, Is.EqualTo(DateTime.MaxValue));
	}

	[Test]
	public void AdvanceTests()
	{
		Crontab cron = Crontab.Parse("@daily");
		cron.PointInTime = DateTime.UnixEpoch.AddSeconds(1);
		DateTime next =cron.AdvancePointInTime();
		Assert.That(cron.PointInTime, Is.EqualTo(DateTime.UnixEpoch.AddDays(1)));
		Assert.That(cron.PointInTime, Is.EqualTo(next));
		Assert.That(cron.NextPointInTime, Is.EqualTo(next.AddDays(1)));
			
		next =cron.AdvancePointInTime();
		Assert.That(cron.PointInTime, Is.EqualTo(DateTime.UnixEpoch.AddDays(2)));
		Assert.That(cron.PointInTime, Is.EqualTo(next));
		Assert.That(cron.NextPointInTime, Is.EqualTo(next.AddDays(1)));
	}
		
	[Test]
	public void EqualityTests()
	{
		Crontab? cron = Crontab.Parse("@yearly");
		cron.Should().NotBeNull();
		
		Assert.That(cron.Equals(cron), Is.True);
		Assert.That(cron == cron, Is.True);
		Assert.That(cron, Is.EqualTo(cron));
		
		Assert.That(cron.Equals(null), Is.False);
		Assert.That(cron == null, Is.False);
		Assert.That(cron, Is.Not.EqualTo(null));
	}
		
}