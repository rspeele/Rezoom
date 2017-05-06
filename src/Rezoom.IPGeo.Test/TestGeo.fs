module Rezoom.IPGeo.Test.TestGeo
open Rezoom
open Rezoom.IPGeo
open NUnit.Framework
open FsUnit

let googleDNS = "8.8.8.8"
let googleDNS2 = "8.8.4.4"
let openDNS = "208.67.220.220"
let fb = "2a03:2880:2110:df07:face:b00c::1"

let googleDNSISP = "Level 3 Communications"
let googleDNS2ISP = "Level 3 Communications"
let openDNSISP = "OpenDNS, LLC"
let fbISP = "Facebook"

[<Test>]
let ``batches`` () =
    {   Task =
            plan {
                let! g1, g2 = Geo.Locate(googleDNS), Geo.Locate(googleDNS2)
                let! o = Geo.Locate(openDNS)
                return g1.Isp, g2.Isp, o.Isp
            }
        Batches =
            [   [googleDNS; googleDNS2]
                [openDNS]
            ]
        ExpectedResult = Value (googleDNSISP, googleDNS2ISP, openDNSISP)
    } |> test

[<Test>]
let ``caching`` () =
    {   Task =
            plan {
                let! g1 = Geo.Locate(googleDNS)
                let! g2, o = Geo.Locate(googleDNS), Geo.Locate(openDNS)
                return g1.Isp, g2.Isp, o.Isp
            }
        Batches =
            [   [googleDNS]
                [] // empty batch because we defer openDNS after getting google from the cache
                [openDNS]
            ]
        ExpectedResult = Value (googleDNSISP, googleDNSISP, openDNSISP)
    } |> test

[<Test>]
let ``dedup`` () =
    {   Task =
            plan {
                let! g1, fb1, fb2 = Geo.Locate(googleDNS), Geo.Locate(fb), Geo.Locate(fb)
                return g1.Isp, fb1.Isp, fb2.Isp
            }
        Batches =
            [   [googleDNS; fb] // note that we don't request fb twice
            ]
        ExpectedResult = Value (googleDNSISP, fbISP, fbISP)
    } |> test