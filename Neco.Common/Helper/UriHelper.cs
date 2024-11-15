namespace Neco.Common.Helper;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using Neco.Common.Data.Hash;
using Neco.Common.Extensions;

public static class UriHelper {
	internal static readonly Uri ExampleBaseUri = new("https://example.org", UriKind.Absolute);

	// TODO: dynamic from cananonical link
	// From https://stackoverflow.com/q/76372936
	private static readonly String[] _thirdPartyQueryParameters = ["_ga", "_ga-ft", "_gl", "_hsmi", "_ke", "_kx", "_paged", "_sm_byp", "_sp", "_szp", "3x", "a", "a_k", "ac", "acpage", "action-box", "action_object_map", "action_ref_map", "action_type_map", "activecampaign_id", "ad", "ad_frame_full", "ad_frame_root", "ad_name", "adclida", "adid", "adlt", "adsafe_ip", "adset_name", "advid", "aff_sub2", "afftrack", "afterload", "ak_action", "alt_id", "am", "amazingmurphybeds", "amp;", "amp;amp", "amp;amp;amp", "amp;amp;amp;amp", "amp;utm_campaign", "amp;utm_medium", "amp;utm_source", "ampStoryAutoAnalyticsLinker", "ampstoryautoanalyticslinke", "an", "ap", "ap_id", "apif", "apipage", "as_occt", "as_q", "as_qdr", "askid", "atFileReset", "atfilereset", "aucid", "auct", "audience", "author", "awt_a", "awt_l", "awt_m", "b2w", "back", "bannerID", "blackhole", "blockedAdTracking", "blog-reader-used", "blogger", "br", "bsft_aaid", "bsft_clkid", "bsft_eid", "bsft_ek", "bsft_lx", "bsft_mid", "bsft_mime_type", "bsft_tv", "bsft_uid", "bvMethod", "bvTime", "bvVersion", "bvb64", "bvb64resp", "bvplugname", "bvprms", "bvprmsmac", "bvreqmerge", "cacheburst", "campaign", "campaign_id", "campaign_name", "campid", "catablog-gallery", "channel", "checksum", "ck_subscriber_id", "cmplz_region_redirect", "cmpnid", "cn-reloaded", "code", "comment", "content_ad_widget", "cost", "cr", "crl8_id", "crlt.pid", "crlt_pid", "crrelr", "crtvid", "ct", "cuid", "daksldlkdsadas", "dcc", "dfp", "dm_i", "domain", "dosubmit", "dsp_caid", "dsp_crid", "dsp_insertion_order_id", "dsp_pub_id", "dsp_tracker_token", "dt", "dur", "durs", "e", "ee", "ef_id", "el", "env", "erprint", "et_blog", "exch", "externalid", "fb_action_ids", "fb_action_types", "fb_ad", "fb_source", "fbclid", "fbzunique", "fg-aqp", "fireglass_rsn", "fo", "fp_sid", "fpa", "fref", "fs", "furl", "fwp_lunch_restrictions", "ga_action", "gclid", "gclsrc", "gdffi", "gdfms", "gdftrk", "gf_page", "gidzl", "goal", "gooal", "gpu", "gtVersion", "haibwc", "hash", "hc_location", "hemail", "hid", "highlight", "hl", "home", "hsa_acc", "hsa_ad", "hsa_cam", "hsa_grp", "hsa_kw", "hsa_mt", "hsa_net", "hsa_src", "hsa_tgt", "hsa_ver", "ias_campId", "ias_chanId", "ias_dealId", "ias_dspId", "ias_impId", "ias_placementId", "ias_pubId", "ical", "ict", "ie", "igshid", "im", "ipl", "jw_start", "jwsource", "k", "key1", "key2", "klaviyo", "ksconf", "ksref", "l", "label", "lang", "ldtag_cl", "level1", "level2", "level3", "level4", "li_fat_id", "limit", "lng", "load_all_comments", "lt", "ltclid", "ltd", "lucky", "m", "m?sales_kw", "matomo_campaign", "matomo_cid", "matomo_content", "matomo_group", "matomo_keyword", "matomo_medium", "matomo_placement", "matomo_source", "max-results", "mc_cid", "mc_eid", "mdrv", "mediaserver", "memset", "mibextid", "mkcid", "mkevt", "mkrid", "mkwid", "ml_subscriber", "ml_subscriber_hash", "mobileOn", "mode", "month", "msID", "msclkid", "msg", "mtm_campaign", "mtm_cid", "mtm_content", "mtm_group", "mtm_keyword", "mtm_medium", "mtm_placement", "mtm_source", "murphybedstoday", "mwprid", "n", "native_client", "navua", "nb", "nb_klid", "o", "okijoouuqnqq", "org", "pa_service_worker", "partnumber", "pcmtid", "pcode", "pcrid", "pfstyle", "phrase", "pid", "piwik_campaign", "piwik_keyword", "piwik_kwd", "pk_campaign", "pk_keyword", "pk_kwd", "placement", "plat", "platform", "playsinline", "pp", "pr", "prid", "print", "q", "q1", "qsrc", "r", "rd", "rdt_cid", "redig", "redir", "ref", "reftok", "relatedposts_hit", "relatedposts_origin", "relatedposts_position", "remodel", "replytocom", "reverse-paginate", "rid", "rnd", "rndnum", "robots_txt", "rq", "rsd", "s_kwcid", "sa", "safe", "said", "sales_cat", "sales_kw", "sb_referer_host", "scrape", "script", "scrlybrkr", "search", "sellid", "sersafe", "sfn_data", "sfn_trk", "sfns", "sfw", "sha1", "share", "shared", "showcomment", "si", "sid", "sid1", "sid2", "sidewalkShow", "sig", "site", "site_id", "siteid", "slicer1", "slicer2", "source", "spref", "spvb", "sra", "src", "srk", "srp", "ssp_iabi", "ssts", "stylishmurphybeds", "subId1", "subId2", "subId3", "subid", "swcfpc", "tail", "teaser", "test", "timezone", "toWww", "triplesource", "trk_contact", "trk_module", "trk_msg", "trk_sid", "tsig", "turl", "u", "up_auto_log", "upage", "updated-max", "uptime", "us_privacy", "usegapi", "usqp", "utm", "utm_campa", "utm_campaign", "utm_content", "utm_expid", "utm_id", "utm_medium", "utm_reader", "utm_referrer", "utm_source", "utm_sq", "utm_ter", "utm_term"];

	private static readonly FrozenSet<String> _superfluousQueryParams = new[] {
		"PHPSESSID", "sid", /* session ids */
		"cid", "client_id", /* client/customer id */
		"utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", /* Urchin Trafic Monitor */
		"fbclid", "msclkid", "gclid", "li_fat_id", "dclid", /* click identifier */
		/* redirects */
		"returnto", "redirect", "redirect_uri",
		/* wikipedia */
		"useskin",
		/* other */
		"MID", "trk", "TITLE_SEO", "search_term", "affiliateid", "avgaffiliate", "cjevent", "wpam_id",
	}.Union(_thirdPartyQueryParameters.Where(p => p.Length > 1)).ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	public static String NormalizeUri(Uri uri) {
		ReadOnlySpan<Char> host;
		String query;
		String path;
		if (uri.IsAbsoluteUri) {
			host = uri.IdnHost;
			query = uri.GetComponents(UriComponents.Query | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
			path = uri.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
		} else {
			host = String.Empty;
			Uri absUri = new(ExampleBaseUri, uri);
			query = absUri.GetComponents(UriComponents.Query | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
			path = absUri.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
		}

		 
		if (host.StartsWith("www."))
			host = host.Slice(4);

		NameValueCollection queryCollection = HttpUtility.ParseQueryString(query);
		IEnumerable<String> queryParams = queryCollection
			.AllKeys
			.Order()
			.WhereNotNull()
			.Where(key => !_superfluousQueryParams.Contains(key))
			.Select(key => $"{key}={queryCollection[key]}");


		return $"{host}{path}?{String.Join('&', queryParams)}";
	}

	public static UInt64 HashUri(Uri uri) {
		String normalizedUri = NormalizeUri(uri);
		Byte[] normalizedBytes = Encoding.UTF8.GetBytes(normalizedUri);
		return WyHashFinal3.HashOneOffLong(normalizedBytes);
	}
}