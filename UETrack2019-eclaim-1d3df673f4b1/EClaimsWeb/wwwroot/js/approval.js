(function ($) {
    'use strict';
    $(function () {
        var val = 0;
        let template = null;
        $('.modal').on('show.bs.modal', function (event) {
            template = $(this).html();
        });

        $('.modal').on('hidden.bs.modal', function (e) {
            $(this).html(template);
        });

        function UpdateApprover() {
            val = $("#txtid").val();
            //     alert('val ' + val);
            //var data = { AMID: 1, Verifier: 3, Approver: 9, AmountFrom: 5656, AmountTo: 8989 };
            //var formData = new FormData();
            //formData.append('data', JSON.stringify(data));
            //var data = { AMID: 1, Verifier: 3, Approver: 9, AmountFrom: 5656, AmountTo: 8989 };
            var formData = new FormData();
            //formData.append('data', JSON.stringify(data));

            var clsApprovers = new Array();
            //var clsApprover = {};
            $('#tblOne tbody').find('tr').each(function (i) {
                var $tds = $(this).find('td');
                ddlVerifier = $tds.find('#ddlVerifier').val();
                ddlApprover = $tds.find('#ddlApprover').val();
                AmountFrom = $tds.find('#txtAmountFrom').val();
                AmountTo = $tds.find('#txtAmountTo').val();
                var clsApprover = {};
                //clsApprover.ApprovalLevels = 4;
                //clsApprover.VerificationLevels = 6;
                clsApprover.AMID = val;
                clsApprover.Verifier = ddlVerifier;
                clsApprover.Approver = ddlApprover;
                clsApprover.AmountFrom = AmountFrom;
                clsApprover.AmountTo = AmountTo;

                //alert('clsApprover ' + clsApprover);
                clsApprovers.push(clsApprover);
            });
            formData.append('data', JSON.stringify(clsApprovers));
            if (clsApprovers.length != 0) {
                //var listdata1 = JSON.stringify({ 'listdata': Params1 });
                //alert('listdata1 ' + clsApprovers);
                $.ajax({
                    type: "POST",
                    url: "/Approval/SaveApprover",
                    data: formData,
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    processData: false,
                    contentType: false,
                    // data: clsApprovers,
                    //data: { clsApprover: JSON.stringify(clsApprover) },
                    //contentType: "application/json; charset=utf-8",
                    //dataType: "json",
                    //contentType: 'application/json; charset=utf-8',
                    //dataType: 'json',
                    //type: 'POST',
                    //url: "/ApprovalSettings/SaveApprover",
                    //data: listdata1,
                    success: function (data) {
                        if (data) {
                            debugger;
                            //alert("Successfully saved");
                            //$("#btnsaveUser").trigger( "click" );
                            // $("#dialog1").dialog('close')
                            //$("#tblOne").find("tr:not(:nth-child(1)):not(:nth-child(1))").remove();
                            //deactivate();
                            //$('#defaultModal').modal({ backdrop: 'static', keyboard: false })
                            //alert('val inside ' + val);
                            //$("#defaultModal").modal("hide");
                            //$("#defaultModal").modal("dispose");
                            Updatedata();
                            alert("Dt details saved successfully");

                            //$("#msgspan").fadeIn();
                            //$("#NotificationMsg").html('Approver Added Successfully');
                            //$("#msgspan").fadeOut(1500);
                        }

                    }
                });

                //window.location.reload();
            }
        }

        function Updatedata() {
            debugger;
            var Params = new Array();


            var formData = new FormData();
            $('#order-listing tbody').find('tr').each(function (i) {

                var $tds = $(this).find('td'),
                    productId = $tds.eq(0).text(),
                    productId = productId.trim();
                ////alert('$("#txtid").val() ' + $("#txtid").val());
                //var val = $("#txtid").val();
                //alert('before save ' + val);
                if (val) {

                    // var status = $(this).find('#status');
                    var status = $tds.find("#ApprovalRequired").toggle(this.checked);
                    if (status.is(':checked')) {

                        active = true;

                    }
                    else {
                        active = false;
                    }

                    var Verification = $tds.find('#VerificationLevels').val();
                    var Approval = $tds.find('#ApprovalLevels').val();

                    Params.push({ AMID: val, ApprovalRequired: active, VerificationLevels: Verification, ApprovalLevels: Approval });

                }


            });
            formData.append('data', JSON.stringify(Params));
            //var listdata = JSON.stringify({ 'listdata': Params });
            $.ajax({
                type: "POST",
                url: "/Approval/SaveItems",
                data: formData,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                processData: false,
                contentType: false,
                success: function (data) {
                    if (data) {
                        alert('Approval Matrix Updated Successfully');
                        //$("#msgspan").fadeIn();
                        //$("#NotificationMsg").html('Approval Matrix Updated Successfully');
                        //$("#msgspan").fadeOut(1500);
                    }
                }
            });
            //data.DataTable({
            //    "sPaginationType": "bootstrap",
            //    "sDom": "<'tableHeader'<l><'clearfix'f>r>t<'tableFooter'<i><'clearfix'p>>",
            //    "iDisplayLength": 10
            //});

            //  window.location.reload();
            //var oTable = $('#tblApproval').dataTable();
            //oTable.fnFilter();

        }

        function isNumber(evt) {
            //alert("number");
            evt = (evt) ? evt : window.event;
            var charCode = (evt.which) ? evt.which : evt.keyCode;
            if (charCode > 31 && (charCode < 47 || charCode > 57) && charCode != 44) {
                return false;
            }
        }

        function EditUser(args) {
            $(function () {
                // var ddlApprover = $("#ddlApprover");
                //  ddlApprover.empty().append('<option selected="selected" value="0" disabled = "disabled">Loading.....</option>');
                $.ajax({
                    type: "POST",
                    url: "/Approval/ReturnJSONDataToAJax",
                    data: '{}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        $(response).each(function () {
                            //alert('binding dropdown values');
                            $("#ddlVerifier").append($("<option></option>").val(this.userID).html(this.name));
                            $("#ddlApprover").append($("<option></option>").val(this.userID).html(this.name));
                        });

                        var Verification;
                        var V;
                        var A;
                        //alert(args);
                        debugger;
                        $("#txtid").val(args);
                        //alert($("#txtid").val());
                        $('#order-listing tbody').find('tr').each(function (i) {
                            var $tds = $(this).find('td'),
                                productId = $tds.eq(0).text(),
                                productId = productId.trim();
                            V = $tds.find('#VerificationLevels').val();
                            A = $tds.find('#ApprovalLevels').val();

                            if (parseInt(V) > parseInt(A)) {
                                Verification = V;
                            }
                            else {
                                Verification = A;
                            }
                            //alert('V ' + V);
                            //alert('A ' + A);
                            //alert('Verification ' + Verification);

                        });
                        if (Verification > 1) {
                            var message = "";
                            var table = document.getElementById("tblOne");
                            var row = table.rows[1];
                            for (var k = 0; k < Verification - 1; k++) {
                                var message = "";
                                for (var j = 0; j < row.cells.length; j++) {
                                    debugger;

                                    if (j == 0) {
                                        if (k < V - 1) {
                                            var cell = row.cells[j];
                                            message += "<td  valign='middle' align='center' >" + cell.innerHTML + "</td>";
                                        }
                                        else {
                                            message += "<td  valign='middle' align='center' ></td>";
                                        }
                                    }
                                    else if (j == 1) {
                                        if (k < A - 1) {
                                            var cell = row.cells[j];
                                            message += "<td  valign='middle' align='center' >" + cell.innerHTML + "</td>";
                                        }
                                        else {
                                            message += "<td  valign='middle' align='center' ></td>";
                                        }
                                    }
                                    else {
                                        var cell = row.cells[j];
                                        message += "<td  valign='middle' align='center' >" + cell.innerHTML + "</td>";
                                    }


                                }
                                $("#tblOne").append('<tr>' + message + '</tr>');

                            }

                            message = "";
                        }
                        $.getJSON("@Url.Content('~/Approval/getdata/')" + args, function (data) {
                            if (data != null && data.length != 0) {
                                var len = data.length;
                                //alert('len ' + len);
                                var values = [];
                                for (var i = 0; i < len; i++) {

                                    $('#tblOne tbody').find('tr').each(function (j) {
                                        debugger;
                                        if (i == j) {
                                            var $tds = $(this).find('td');

                                            if (V == "0") {
                                                $tds.find('#ddlVerifier').prop("disabled", false);
                                                $tds.find('#ddlVerifier').prop('selectedIndex', 0);
                                                if (data[i].verifier != 0) {
                                                    $tds.find('#ddlVerifier').val(data[i].verifier);
                                                }
                                            }
                                            else if (data[i].verifier == 0) {
                                                $tds.find('#ddlVerifier').prop('selectedIndex', 0);
                                            }
                                            else {
                                                $tds.find('#ddlVerifier').val(data[i].verifier);
                                            }
                                            if (A == "0") {
                                                $tds.find('#ddlApprover').prop("disabled", false);
                                                $tds.find('#ddlApprover').prop('selectedIndex', 0);
                                            }
                                            else if (data[i].approver == 0) {
                                                $tds.find('#ddlApprover').prop('selectedIndex', 0);
                                            }
                                            else {
                                                $tds.find('#ddlApprover').val(data[i].approver);
                                            }
                                            $tds.find('#txtAmountFrom').val(data[i].amountFrom);
                                            $tds.find('#txtAmountTo').val(data[i].amountTo);
                                        }
                                    });
                                }

                            }
                            else {
                                $('#tblOne tbody').find('tr').each(function (j) {

                                    var $tds = $(this).find('td');

                                    if (V == "0") {
                                        $tds.find('#ddlVerifier').prop("disabled", false);
                                        $tds.find('#ddlVerifier').prop('selectedIndex', 0);
                                    }
                                    else {
                                        $tds.find('#ddlVerifier').prop('selectedIndex', 0);
                                    }
                                    if (A == "0") {
                                        $tds.find('#ddlApprover').prop("disabled", false);
                                        $tds.find('#ddlApprover').prop('selectedIndex', 0);
                                    }
                                    else {
                                        $tds.find('#ddlApprover').prop('selectedIndex', 0);
                                    }
                                });
                            }
                        });
                        //ddlApprover.empty().append('<option selected="selected" value="0">Please select</option>');
                        //$.each(response, function () {
                        //    if (response.length > 0) {
                        //        $('#ddlApprover').html('');
                        //        var options = '';
                        //        options += '<option value="Select">Select</option>';
                        //        for (var i = 0; i < response.length; i++) {
                        //            options += '<option value="' + response[i] + '">' + response[i] + '</option>';
                        //        }
                        //        $('#ddlApprover').append(options);
                        //    }
                        //});
                    },
                    failure: function (response) {
                        alert(response.responseText);
                    },
                    error: function (response) {
                        alert(response.responseText);
                    }
                });
            });
        }

        $('#ModuleName').on('change', function () {
            getScreensByModule();
        });

        var getScreensByModule = function () {
            $.ajax({
                url: '@Url.Action("GetScreens", "Approval")',
                type: 'GET',
                data: {
                    moduleName: $('#ModuleName').val(),
                },
                success: function (data) {
                    $('#LScreenId').find('option').remove()
                    $(data).each(
                        function (index, item) {
                            $('#LScreenId').append('<option value="' + item.value + '">' + item.text + '</option>')
                        });
                },
                error: function () {
                }
            });
        }
    });
})(jQuery);