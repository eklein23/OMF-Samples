/*

©2009-2018 OSIsoft, LLC. All Rights Reserved.

No Warranty or Liability.  The OSIsoft Samples contained herein are being supplied to Licensee
"AS IS" without any warranty of any kind.  OSIsoft DISCLAIMS ALL EXPRESS AND IMPLIED WARRANTIES,
INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE and NONINFRINGEMENT. In no event will OSIsoft be liable to Licensee or to any third party
for damages of any kind arising from Licensee's use of the OSIsoft Samples OR OTHERWISE, including
but not limited to direct, indirect, special, incidental and consequential damages, and Licensee
expressly assumes the risk of all such damages.  FURTHER, THE OSIsoft SAMPLES ARE NOT ELIGIBLE FOR
SUPPORT UNDER EITHER OSISOFT'S STANDARD OR ENTERPRISE LEVEL SUPPORT AGREEMENTS

*/

/* sendToServerC.c */

/* The functions in this file handle communications with the Relay */

#include <string.h>

#define CURL_STATICLIB
#include <curl/curl.h>

#include "bufferData.h"
#include "sendToServerC.h"
#include "main.h"

static CURL* g_curl;
static char g_errbuf[CURL_ERROR_SIZE];

extern int certCheck; /* Verify certificate and hostname of the endpoint */

char* getCommLibErr()
{
	static char errbufNL[CURL_ERROR_SIZE + 1];
	snprintf(errbufNL, CURL_ERROR_SIZE + 1, "%s%s", g_errbuf, "\n");

	return errbufNL;
}

/*
   Returns
      0 -- successfully initialized communications library
	 -1 -- could not initialize communications library
*/
int initCommLib()
{
	int retval = -1 ;
	
	if (0 == curl_global_init(CURL_GLOBAL_ALL)) {
		g_curl = curl_easy_init();
		if (g_curl) {
			retval = 0;
		}
	}

	return retval;
}

void endCommLib()
{
	curl_easy_cleanup(g_curl);
	curl_global_cleanup();
}

/*
   Returns
      0 -- successfull setup of the buffer file
	 -1 -- could not set up the buffer file
*/
int setUpBuffering()
{
	int retval = 0;

	SetBufferFileName();

	if (0 == NeedToCreateBufferFile()) {
#ifdef _DEBUG
		printf("using existing buffer file\n");
#endif
	}
	else if (CreateBufferFile()) {
		printf("CreateBufferFile() failed\n");
		retval = -1;
	}

	return retval;
}

#define LEN_HDR_ITEM 1024
#define PARAM_LENGTH  256
static char g_szUrl[LEN_URL];
static char g_producerid[PARAM_LENGTH];
static char g_version[PARAM_LENGTH];
static char g_msgformat[PARAM_LENGTH];

/* Function to supress output of curl messages */
size_t noop_cb(void* ptr, size_t size, size_t nmemb, void* data) 
{
	return size * nmemb;
}

/* This function sets values for the parameters that are constant for
   every POST to the Relay.
*/
void setServerParams(
	const char* hostAndPort,
	const char* page,
	const char* producerid,
	const char* version,
	const char* msgformat
	)
{
        /* Updated for 201803beta */
	snprintf(g_szUrl, LEN_URL, "https://%s%s", hostAndPort, page);

	snprintf(g_producerid, PARAM_LENGTH, "%s", producerid);
	snprintf(g_version, PARAM_LENGTH, "%s", version);
	snprintf(g_msgformat, PARAM_LENGTH, "%s", msgformat);
}

/* This function generates a key/value pair that we send in the HTTP header */
char* generateHeaderItem(const char* szKey, const char* szVal)
{
	static char szRet[LEN_HDR_ITEM];

	snprintf(szRet, LEN_HDR_ITEM, "%s: %s", szKey, szVal);

	return szRet;
}

/* This function sends a message to the Relay
   Returns
	  0 -- message succesfully sent to Relay
	  ERR_CouldNotConnect
   Sets
      http_response_code
	     1 -- if response code is 200 or 202
	     0 -- otherwise
*/
int sendMsgToRelay(
	int* http_response_code,
	const char* msgtype,  /* part of the message header */
	const char* action,  /* part of the message header */
	const char* poststr)
{
	CURLcode res;

	struct curl_slist* headers = NULL;
	int retval = 0;
	*http_response_code = 0;

	curl_easy_setopt(g_curl, CURLOPT_URL, g_szUrl);
	curl_easy_setopt(g_curl, CURLOPT_ERRORBUFFER, g_errbuf);
	curl_easy_setopt(g_curl, CURLOPT_WRITEFUNCTION, noop_cb);

	/*
	The libCurl documentation indicates:
	CURLOPT_SSL_VERIFYPEER -- Verify the SSL certificate.
	CURLOPT_SSL_VERIFYHOST -- Verify the host name in the SSL certificate.
	CURLOPT_CAINFO -- Pass a file holding a certificate to verify the peer with
	*/

	/*** 
	so, to enable verification of the Relay computer, comment out the if 0 here, and uncomment on line 169 
 #if 0 
	curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYPEER, 1);
	curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYHOST, 2);
	curl_easy_setopt(g_curl, CURLOPT_CAINFO, GetRelayCertFile());
 #endif 

#if 1
	 so, to disable verification of the Relay computer, comment out the if 0 here, and uncomment on 163 
	curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYPEER, 0);
	curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYHOST, 0);
#endif

	***/

	if (certCheck) {
		/* Verify certificate and host */
		curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYPEER, 1);
		curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYHOST, 2);
	}
	else {
		/* Disable certificate check */
		curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYPEER, 0);
		curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYHOST, 0);
	}

	headers = curl_slist_append(headers, generateHeaderItem("producertoken", g_producerid));
	headers = curl_slist_append(headers, generateHeaderItem("omfversion", g_version));
	headers = curl_slist_append(headers, generateHeaderItem("messageformat", g_msgformat));
	headers = curl_slist_append(headers, generateHeaderItem("messagetype", msgtype));
	headers = curl_slist_append(headers, generateHeaderItem("action", action));

	curl_easy_setopt(g_curl, CURLOPT_HTTPHEADER, headers);
	curl_easy_setopt(g_curl, CURLOPT_POSTFIELDS, poststr);

	res = curl_easy_perform(g_curl);
	curl_easy_getinfo(g_curl, CURLINFO_RESPONSE_CODE, http_response_code);
	printf("Got curl res: %s and response code: %d\n", curl_easy_strerror(res), *http_response_code);
	curl_slist_free_all(headers); /* Required  to free headers list at some point! */
	headers = NULL;

	if (CURLE_OK == res) {
		if (200 == *http_response_code || 202 == *http_response_code || 204 == *http_response_code) {
			retval = 0;
		}
		else if (503 == *http_response_code) {
			/* 503 is Service Unavailable; treat as cannot connect */
			snprintf(g_errbuf, CURL_ERROR_SIZE, "%s", "Service Unavailable");
			retval = ERR_CouldNotConnect;
		}
	}
	else if (CURLE_COULDNT_CONNECT == res) {
		retval = ERR_CouldNotConnect;
	}

	return retval;
}

/*
This function processes a POST message. We first check wether the buffer file has data.

If the buffer file has not data, then we call sendMsgToRelay(). If we cannot connect to
the Relay, we serialize the data.

If the buffer has data, then we
   serialize the data to the end of the buffer file
   deserialize data from the buffer file and call sendMsgToRelay().

Returns
   return value of sendMsgToRelay() if buffer file has no data
   INFO_WROTE_MSG_TO_BUFFER if buffer file has data

*/
int processPostMsg(
	int* http_response_codeParam,
	const char* msgtype,  /* part of the message header */
	const char* action,  /* part of the message header */
	const char* poststr)
{
	int retval = 0;

	*http_response_codeParam = 0;

	int ebf = ExamineBufferFile();
	if (FILE_OPEN_ERR == ebf) {
		/* need code to handle this case*/
	}
	else if (BUFFER_FILE_NO_DATA == ebf) {
		/* existing buffer file does not have data */
		int http_response_code;

		/* so, we send the message to the Relay */
		retval = sendMsgToRelay(&http_response_code, msgtype, action, poststr);
		if (0 == retval) {
			*http_response_codeParam = http_response_code;
		}
		else {
			if (ERR_CouldNotConnect == retval) {
				/* if we cannot connect to the Relay, we write the data to the buffer file */
				SerializeData(msgtype, action, poststr);
			}
		}		
	}
	else if (BUFFER_FILE_HAS_DATA == ebf) {
		/* existing buffer file has data */

		/* write the data to the end of the buffer file */
		SerializeData(msgtype, action, poststr);

		retval = INFO_WROTE_MSG_TO_BUFFER;
	}

	return retval;
}
