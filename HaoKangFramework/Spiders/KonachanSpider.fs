﻿module HaoKangFramework.Spiders.KonachanSpider

open HaoKangFramework

module internal KonachanSpiderUtils =
    open SpiderUtils

    let PageLimit = 100

    let Rating = function
    | "s" -> Everyone
    | "q" -> R15
    | "e" -> R18
    | _ -> Unknown

    type private RequestFormat =
        Printf.StringFormat<(string -> int -> int -> string -> string)>

    type UrlFixer = string -> string

    let internal ApplyUrl format xmlUrl pageIndex tags =
        sprintf format xmlUrl PageLimit pageIndex (ReduceTags tags)

    let internal GetPage (xmlUrl:string) (pageIndex:int) tags format urlFixer spider =
        try
            let xmlDoc =
                ApplyUrl format xmlUrl pageIndex tags
                |> DownloadXml

            let posts = xmlDoc.SelectSingleNode "posts"

            seq { 
                for i in posts.ChildNodes ->
                    let url = i.Attributes.["file_url"] |> UnwrapXmlValue |> urlFixer
                    {
                        ID = i.Attributes.["id"] |> UnwrapXmlValue |> uint64
                        PreviewImage = i.Attributes.["preview_url"] 
                                        |> UnwrapXmlValue 
                                        |> urlFixer 
                                        |> DownloadDataLazy
                        Content = [{
                            Data = url |> DownloadDataLazy
                            FileName =  url.[url.LastIndexOf '/' + 1 ..]
                            FileExtName = url.[url.LastIndexOf '.' + 1 ..]
                            Url = url }]
                        AgeGrading = i.Attributes.["rating"] |> UnwrapXmlValue |> Rating
                        Author = i.Attributes.["author"] |> UnwrapXmlValue
                        Tags = (i.Attributes.["tags"] |> UnwrapXmlValue).Trim().Split ' '
                        FromSpider = spider }}
            |> Ok
        with e -> Error e

    let KonachanFormat : RequestFormat = "%s?limit=%d&page=%d&tags=%s"
    let GelbooruFormat : RequestFormat = "%s?page=dapi&s=post&q=index&&limit=%d&pid=%d&tags=%s"

    let NoFixer x = x
    let HttpsFixer x = "https:" + x
    
    type KonachanSpider (spiderName,xmlUrl,requestFormat,pageGetter,urlFixer) =
        inherit obj ()
        override x.ToString () =
            spiderName + " Spider"

        interface ISpider with
            member x.Dispose () = ()
            member x.TestConnection () =
                try
                    pageGetter xmlUrl 1 [] requestFormat urlFixer x
                    |> function
                    | Ok _ -> Ok ()
                    | Error e -> raise e

                with e -> Error e
            
            member x.Search(tags:string list): Result<Page,exn> seq = 
                Seq.initInfinite (fun i ->
                    pageGetter xmlUrl i tags requestFormat urlFixer x)


open KonachanSpiderUtils

[<Spider>]
let Konachan =
    new KonachanSpider ("Konachan","http://konachan.net/post.xml",KonachanFormat,GetPage,NoFixer) 
    :> ISpider

[<Spider>]
let Lolibooru =
    new KonachanSpider ("Lolibooru","https://lolibooru.moe/post.xml",KonachanFormat,GetPage,NoFixer)
    :> ISpider

[<Spider>]
let HypnoHub =
    new KonachanSpider ("HypnoHub","https://hypnohub.net/post/index.xml",KonachanFormat,GetPage,HttpsFixer)
    :> ISpider

[<Spider>]
let Gelbooru =
    new KonachanSpider ("Gelbooru","https://www.youhate.us/index.php",GelbooruFormat,GetPage,NoFixer)
    :> ISpider

[<Spider>]
let Rule34 =
    new KonachanSpider ("Rule34","https://rule34.xxx/index.php",GelbooruFormat,GetPage,NoFixer)
    :> ISpider

[<Spider>]
let SafeBooru =
    new KonachanSpider ("SafeBooru","https://safebooru.org/index.php",GelbooruFormat,GetPage,HttpsFixer)
    :> ISpider

//[<Spider>]
let Yandere =
    new KonachanSpider ("Yandere","https://yande.re/post.xml",KonachanFormat,GetPage,NoFixer)
    :> ISpider
