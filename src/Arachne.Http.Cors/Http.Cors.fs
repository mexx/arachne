﻿//----------------------------------------------------------------------------
//
// Copyright (c) 2014
//
//    Ryan Riley (@panesofglass) and Andrew Cherry (@kolektiv)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//----------------------------------------------------------------------------

module Arachne.Http.Cors

open System
open Arachne.Core
open Arachne.Http
open Arachne.Uri
open FParsec

(* RFC 6454

   Types, parsers and formatters implemented to mirror the specification of 
   Origin semantics as defined in RFC 6454.

   Taken from [http://tools.ietf.org/html/rfc6454] *)

(* Origin

   Taken from RFC 6454, Section 7 Origin
   See [http://tools.ietf.org/html/rfc6454#section-7]

   Also described as part of the W3C Recommendation on CORS, Section 5.7
   See [http://www.w3.org/TR/2014/REC-cors-20140116/#syntax] *)

type Origin =
    | Origin of OriginListOrNull

    static member internal Mapping =

        let originP =
            OriginListOrNull.Mapping.Parse |>> Origin

        let originF =
            function | Origin x -> OriginListOrNull.Mapping.Format x

        { Parse = originP
          Format = originF }

    static member format =
        Mapping.format Origin.Mapping

    static member parse =
        Mapping.parse Origin.Mapping

    static member tryParse =
        Mapping.tryParse Origin.Mapping

    override x.ToString () =
        Origin.format x

and OriginListOrNull =
    | Origins of SerializedOrigin list
    | Null

    static member internal Mapping =

        let originListOrNullP =
            choice [
                attempt (sepBy1 SerializedOrigin.Mapping.Parse (skipChar ' ')) |>> Origins
                skipString "null" >>% Null ]

        let originListOrNullF =
            function | Origins x -> join SerializedOrigin.Mapping.Format (append " ") x
                     | Null -> append "null"

        { Parse = originListOrNullP
          Format = originListOrNullF }

and SerializedOrigin =
    | SerializedOrigin of Scheme * Host * Port option

    static member internal Mapping =

        let serializedOriginP =
                 Scheme.Mapping.Parse .>> skipString "://" 
            .>>. Host.Mapping.Parse
            .>>. opt Port.Mapping.Parse
             |>> fun ((scheme, host), port) ->
                SerializedOrigin (scheme, host, port)

        let serializedOriginF =
            function | SerializedOrigin (s, h, p) ->
                            let formatters =
                                [ Scheme.Mapping.Format s
                                  append "://"
                                  Host.Mapping.Format h
                                  (function | Some p -> Port.Mapping.Format p
                                            | _ -> id) p ]

                            fun b -> List.fold (|>) b formatters

        { Parse = serializedOriginP
          Format = serializedOriginF }

(* W3C Recommendation on CORS

   Types, parsers and formatters implemented to mirror the specification of 
   CORS semantics as defined in W3C Recommendation on CORS (version dated 20140116).

   Taken from [http://www.w3.org/TR/2014/REC-cors-20140116] *)

(* Access-Control-Allow-Origin

   Taken from W3C Recommendation on CORS, Section 5.1 Access-Control-Allow-Origin
   See [http://www.w3.org/TR/2014/REC-cors-20140116/#syntax] *)

type AccessControlAllowOrigin =
    | AccessControlAllowOrigin of AccessControlAllowOriginRange

    static member internal Mapping =

        let accessControlAllowOriginP =
            choice [
                attempt OriginListOrNull.Mapping.Parse |>> (Origins >> AccessControlAllowOrigin)
                skipChar '*' >>% AccessControlAllowOrigin (Any) ]

        let accessControlAllowOriginF =
            function | AccessControlAllowOrigin (Origins x) -> OriginListOrNull.Mapping.Format x
                     | AccessControlAllowOrigin (Any) -> append "*"

        { Parse = accessControlAllowOriginP
          Format = accessControlAllowOriginF }

    static member format =
        Mapping.format AccessControlAllowOrigin.Mapping

    static member parse =
        Mapping.parse AccessControlAllowOrigin.Mapping

    static member tryParse =
        Mapping.tryParse AccessControlAllowOrigin.Mapping

    override x.ToString () =
        AccessControlAllowOrigin.format x

and AccessControlAllowOriginRange =
    | Origins of OriginListOrNull
    | Any

(* Access-Control-Allow-Credentials

   Taken from W3C Recommendation on CORS, Section 5.2 Access-Control-Allow-Credentials
   See [http://www.w3.org/TR/2014/REC-cors-20140116/#syntax] *)

type AccessControlAllowCredentials =
    | AccessControlAllowCredentials

    static member internal Mapping =

        let accessControlAllowCredentialsP =
            skipString "true" >>% AccessControlAllowCredentials

        let accessControlAllowCredentialsF =
            function | AccessControlAllowCredentials -> append "true"

        { Parse = accessControlAllowCredentialsP
          Format = accessControlAllowCredentialsF }

    static member format =
        Mapping.format AccessControlAllowCredentials.Mapping

    static member parse =
        Mapping.parse AccessControlAllowCredentials.Mapping

    static member tryParse =
        Mapping.tryParse AccessControlAllowCredentials.Mapping

    override x.ToString () =
        AccessControlAllowCredentials.format x

(* Access-Control-Expose-Headers

   Taken from W3C Recommendation on CORS, Section 5.3 Access-Control-Expose-Headers
   See [http://www.w3.org/TR/2014/REC-cors-20140116/#syntax] *)

type AccessControlExposeHeaders =
    | AccessControlExposeHeaders of string list

    static member internal Mapping =

        let accessControlExposeHeadersP =
            Grammar.infixP Grammar.tokenP (skipChar ',') |>> AccessControlExposeHeaders

        let accessControlExposeHeadersF =
            function | AccessControlExposeHeaders x -> join append (append ",") x

        { Parse = accessControlExposeHeadersP
          Format = accessControlExposeHeadersF }

    static member format =
        Mapping.format AccessControlExposeHeaders.Mapping

    static member parse =
        Mapping.parse AccessControlExposeHeaders.Mapping

    static member tryParse =
        Mapping.tryParse AccessControlExposeHeaders.Mapping

    override x.ToString () =
        AccessControlExposeHeaders.format x

(* Access-Control-Max-Age

   Taken from W3C Recommendation on CORS, Section 5.4 Access-Control-Max-Age
   See [http://www.w3.org/TR/2014/REC-cors-20140116/#syntax] *)

type AccessControlMaxAge =
    | AccessControlMaxAge of TimeSpan

    static member internal Mapping =

        let accessControlMaxAgeP =
            puint32 |>> (float >> TimeSpan.FromSeconds >> AccessControlMaxAge)

        let accessControlMaxAgeF =
            function | AccessControlMaxAge x -> append (string x.TotalSeconds)

        { Parse = accessControlMaxAgeP
          Format = accessControlMaxAgeF }

    static member format =
        Mapping.format AccessControlMaxAge.Mapping

    static member parse =
        Mapping.parse AccessControlMaxAge.Mapping

    static member tryParse =
        Mapping.tryParse AccessControlMaxAge.Mapping

    override x.ToString () =
        AccessControlMaxAge.format x

(* Access-Control-Allow-Methods

   Taken from W3C Recommendation on CORS, Section 5.5 Access-Control-Allow-Methods
   See [http://www.w3.org/TR/2014/REC-cors-20140116/#syntax] *)

type AccessControlAllowMethods =
    | AccessControlAllowMethods of Method list

    static member internal Mapping =

        let accessControlAllowMethodsP =
            Grammar.infixP Method.Mapping.Parse (skipChar ',') |>> AccessControlAllowMethods

        let accessControlAllowMethodsF =
            function | AccessControlAllowMethods x -> join Method.Mapping.Format (append ",") x

        { Parse = accessControlAllowMethodsP
          Format = accessControlAllowMethodsF }

    static member format =
        Mapping.format AccessControlAllowMethods.Mapping

    static member parse =
        Mapping.parse AccessControlAllowMethods.Mapping

    static member tryParse =
        Mapping.tryParse AccessControlAllowMethods.Mapping

    override x.ToString () =
        AccessControlAllowMethods.format x

(* Access-Control-Allow-Headers

   Taken from W3C Recommendation on CORS, Section 5.6 Access-Control-Allow-Headers
   See [http://www.w3.org/TR/2014/REC-cors-20140116/#syntax] *)

type AccessControlAllowHeaders =
    | AccessControlAllowHeaders of string list

    static member internal Mapping =

        let accessControlAllowHeadersP =
            Grammar.infixP Grammar.tokenP (skipChar ',') |>> AccessControlAllowHeaders

        let accessControlAllowHeadersF =
            function | AccessControlAllowHeaders x -> join append (append ",") x

        { Parse = accessControlAllowHeadersP
          Format = accessControlAllowHeadersF }

    static member format =
        Mapping.format AccessControlAllowHeaders.Mapping

    static member parse =
        Mapping.parse AccessControlAllowHeaders.Mapping

    static member tryParse =
        Mapping.tryParse AccessControlAllowHeaders.Mapping

    override x.ToString () =
        AccessControlAllowHeaders.format x

(* Access-Control-Request-Method

   Taken from W3C Recommendation on CORS, Section 5.8 Access-Control-Request-Method
   See [http://www.w3.org/TR/2014/REC-cors-20140116/#syntax] *)

type AccessControlRequestMethod =
    | AccessControlRequestMethod of Method

    static member internal Mapping =

        let accessControlRequestMethodP =
            Method.Mapping.Parse |>> AccessControlRequestMethod

        let accessControlRequestMethodF =
            function | AccessControlRequestMethod x -> Method.Mapping.Format x

        { Parse = accessControlRequestMethodP
          Format = accessControlRequestMethodF }

    static member format =
        Mapping.format AccessControlRequestMethod.Mapping

    static member parse =
        Mapping.parse AccessControlRequestMethod.Mapping

    static member tryParse =
        Mapping.tryParse AccessControlRequestMethod.Mapping

    override x.ToString () =
        AccessControlRequestMethod.format x

(* Access-Control-Request-Headers

   Taken from W3C Recommendation on CORS, Section 5.9 Access-Control-Request-Headers
   See [http://www.w3.org/TR/2014/REC-cors-20140116/#syntax] *)

type AccessControlRequestHeaders =
    | AccessControlRequestHeaders of string list

    static member internal Mapping =

        let accessControlRequestHeadersP =
            Grammar.infixP Grammar.tokenP (skipChar ',') |>> AccessControlRequestHeaders

        let accessControlRequestHeadersF =
            function | AccessControlRequestHeaders x -> join append (append ",") x

        { Parse = accessControlRequestHeadersP
          Format = accessControlRequestHeadersF }

    static member format =
        Mapping.format AccessControlRequestHeaders.Mapping

    static member parse =
        Mapping.parse AccessControlRequestHeaders.Mapping

    static member tryParse =
        Mapping.tryParse AccessControlRequestHeaders.Mapping

    override x.ToString () =
        AccessControlRequestHeaders.format x
