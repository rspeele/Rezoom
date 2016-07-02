namespace Data.Resumption.IPGeo.Test
open Data.Resumption
open Data.Resumption.IPGeo
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestGeo() =
    static let googleDNS = "8.8.8.8"
    static let googleDNS2 = "8.8.4.4"
    static let openDNS = "208.67.220.220"
    static let fb = "2a03:2880:2110:df07:face:b00c::1"

    static let googleDNSISP = "Google"
    static let googleDNS2ISP = "Level 3 Communications"
    static let openDNSISP = "OpenDNS, LLC"
    static let fbISP = "Facebook"

    [<TestMethod>]
    member __.TestBatches() =
        {
            Task =
                datatask {
                    let! g1, g2 = Geo.Locate(googleDNS), Geo.Locate(googleDNS2)
                    let! o = Geo.Locate(openDNS)
                    return g1.Isp, g2.Isp, o.Isp
                }
            Batches =
                [
                    [googleDNS; googleDNS2]
                    [openDNS]
                ]
            ExpectedResult = Value (googleDNSISP, googleDNS2ISP, openDNSISP)
        } |> test

    [<TestMethod>]
    member __.TestCaching() =
        {
            Task =
                datatask {
                    let! g1 = Geo.Locate(googleDNS)
                    let! g2, o = Geo.Locate(googleDNS), Geo.Locate(openDNS)
                    return g1.Isp, g2.Isp, o.Isp
                }
            Batches =
                [
                    [googleDNS]
                    [openDNS] // note that we don't request google DNS again
                ]
            ExpectedResult = Value (googleDNSISP, googleDNSISP, openDNSISP)
        } |> test

    [<TestMethod>]
    member __.TestDedup() =
        {
            Task =
                datatask {
                    let! g1, fb1, fb2 = Geo.Locate(googleDNS), Geo.Locate(fb), Geo.Locate(fb)
                    return g1.Isp, fb1.Isp, fb2.Isp
                }
            Batches =
                [
                    [googleDNS; fb] // note that we don't request fb twice
                ]
            ExpectedResult = Value (googleDNSISP, fbISP, fbISP)
        } |> test