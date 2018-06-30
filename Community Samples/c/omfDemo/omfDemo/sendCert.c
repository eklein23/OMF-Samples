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

/* sendCert.c */

#ifdef _WIN32
#define _CRT_SECURE_NO_WARNINGS  /* to remove warnings on fopen() */
#include <stdio.h>
#include <windows.h>
#elif linux
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#else
need to implement
#endif

#define CURL_STATICLIB
#include <curl/curl.h>

#include "json.h"
#include "main.h"

/* declartion of functions implemented in other files */
char* generateHeaderItem(const char* szKey, const char* szVal);
char* GetFullPath();
/* end declartion of functions implemented in other files */

#define CERTIFICATE_FILE  "omfConnector.pem"
#define CONNECTOR_TYPE  "OMF Connector"

#define RELAY_END_POINT "relay/api/certificate"

struct MemoryStruct {
	char* memory;
	size_t size;
};

static size_t
WriteMemoryCallback(void* contents, size_t size, size_t nmemb, void* userp)
{
	size_t realsize = size * nmemb;
	struct MemoryStruct* mem = (struct MemoryStruct *) userp;

	mem->memory = realloc(mem->memory, mem->size + realsize + 1);
	if (mem->memory == NULL) {
		/* out of memory! */
		printf("not enough memory (realloc returned NULL)\n");
		return 0;
	}

	memcpy(&(mem->memory[mem->size]), contents, realsize);
	mem->size += realsize;
	mem->memory[mem->size] = 0;

	return realsize;
}

static void showServRet(JsonNode* theData, const char* szKey)
{
	JsonNode* aMember = json_find_member(theData, szKey);
	if (JSON_STRING == aMember->tag) {
		printf("%s: %s\n", szKey, aMember->string_);
	}
}

static int sendCert(const char* szUrl,
	const char* szConnectoryType,
	const char* szCertificateBytes,
	const char* szHostname,
	const char* szOneTimePassword,
	const char* szConnectorUid)
{
	CURL *curl;
	CURLcode res;
	struct curl_slist* headers = NULL;
	struct MemoryStruct chunk;

	char* szTX;
	JsonNode* jObj = json_mkobject();

	chunk.memory = malloc(1);  /* will be grown as needed by the realloc above */
	chunk.size = 0;    /* no data at this point */

	json_append_member(jObj, "ConnectorType", json_mkstring(szConnectoryType));
	json_append_member(jObj, "CertificateBytes", json_mkstring(szCertificateBytes));
	json_append_member(jObj, "Hostname", json_mkstring(szHostname));
	json_append_member(jObj, "OneTimePassword", json_mkstring(szOneTimePassword));
	json_append_member(jObj, "ConnectorUid", json_mkstring(szConnectorUid));
	szTX = json_encode(jObj);

	curl_global_init(CURL_GLOBAL_ALL);
	curl = curl_easy_init();
	if (curl) {
#if 0
		/* so, to disable verification of the Relay computer */
		curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYPEER, 0);
		curl_easy_setopt(g_curl, CURLOPT_SSL_VERIFYHOST, 0);
#endif

		/* so, to enable verification of the Relay computer */
		curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 1);
		curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, 2);
		curl_easy_setopt(curl, CURLOPT_CAINFO, GetRelayCertFile());

		headers = curl_slist_append(headers, generateHeaderItem("Accept", "application/json"));
		headers = curl_slist_append(headers, generateHeaderItem("Content-Type", "application/json"));

		curl_easy_setopt(curl, CURLOPT_URL, szUrl);
		curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteMemoryCallback);
		curl_easy_setopt(curl, CURLOPT_WRITEDATA, (void *) &chunk);

		curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
		curl_easy_setopt(curl, CURLOPT_POSTFIELDS, szTX);

		res = curl_easy_perform(curl);
		if (res == CURLE_OK) {
			long responseCode;
			//JsonNode* aMember;
			JsonNode* theData = json_decode(chunk.memory);

			curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &responseCode);

			if (200 == responseCode) {
				logMsg("Successfully sent certificate to the Relay");

				printf("Relay returned the following data: \n");
				showServRet(theData, "OneTimePassword");
				showServRet(theData, "ConnectorUid");
				/* "CertificateBytes" key gives the Relay's server certificate */
			}
			else {
				char errBuf[CURL_ERROR_SIZE];
				snprintf(errBuf, CURL_ERROR_SIZE, "Error from Relay: %d", responseCode);
				logMsg(errBuf);

				showServRet(theData, "Message");
			}			
		}
		else {
			fprintf(stderr, "curl_easy_perform() failed: %s\n", curl_easy_strerror(res));
		}

		curl_easy_cleanup(curl);
		free(chunk.memory);
		curl_global_cleanup();
	}

	free(szTX);

	return 0;
}

static void sendConnCertToRelay(const char* szUrl, 
	const char* szCertFile, 
	const char* szConnectorType, 
	const char* szHostname, 
	const char* szConnectorUid,
	const char* szOneTimePassword)
{
	char* szPemFileContents;
	char szFileToRead[PATH_LENGTH];

	int numBytes = 0;
	snprintf(szFileToRead, PATH_LENGTH, "%s%s", GetFullPath(), szCertFile);
	printf("Certificate file is %s\n", szFileToRead);

	/* find out the size of the certificate file */
	{
		FILE* file = fopen(szFileToRead, "r");
		if (file) {
			while (getc(file) != EOF) {
				numBytes++;
			}
			fclose(file);
		}
		else {
			printf("Cannot open file: %s\n", szFileToRead);
			return;
		}
	}

	/* allocate memory based on the size of the certificate file */
	szPemFileContents = (char*) malloc(numBytes);
	memset(szPemFileContents, 0, numBytes);
	{
		char line[160];
		FILE* file = fopen(szFileToRead, "r");
		if (file) {
			while (fgets(line, sizeof(line), file)) {
				if (line[0] == '-' && line[1] == '-' && line[2] == '-') {
					;  /* ignore lines such as -----BEGIN CERTIFICATE----- */
				}
				else {
					strncat(szPemFileContents, line, strlen(line) - 1);
				}
			}
			fclose(file);
		}
		else {
			printf("Cannot open file: %s\n", szFileToRead);
			return;
		}
	}

	printf("\nSending certificate to %s\n\n", szUrl);
	sendCert(szUrl, szConnectorType, szPemFileContents, szHostname, szOneTimePassword, szConnectorUid);

	free(szPemFileContents);
}

static char* myComputerName()
{
#ifndef MAX_COMPUTERNAME_LENGTH
#define MAX_COMPUTERNAME_LENGTH 80
#endif
	static char szHostname[MAX_COMPUTERNAME_LENGTH + 1];
	int nSize = MAX_COMPUTERNAME_LENGTH;

#ifdef _WIN32
	GetComputerName(szHostname, &nSize);
#elif linux
	gethostname(szHostname, sizeof(szHostname));
#else
	need to implement
#endif
		
	return szHostname;
}