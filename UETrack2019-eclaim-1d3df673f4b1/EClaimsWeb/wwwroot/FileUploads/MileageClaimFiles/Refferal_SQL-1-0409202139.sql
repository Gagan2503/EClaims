select top 50* from order_ where actclass like '%REF%' order by create_timestamp desc

INSERT INTO [order_] ([enterprise_id],[practice_id],[person_id])VALUES('00001','0001','0C45C2ED-6162-4FB9-9402-AA1A7FB021BD')

INSERT INTO [order_] (INSERT INTO [order_] ([locationName],[orderedBy],[orderedByKey],[orderedDate],[orderedTime],[referToPhysician],[referToSpecialty],[userID])VALUES('Family Practice Location','Matthew IM Abbott MD','C153AB5E-9BE3-47A2-A648-8C14CB0B276E','20200319','9:51 AM','Ernest Bodie MD','Obstetrics','0'))VALUES('Family Practice Location','Matthew IM Abbott MD','C153AB5E-9BE3-47A2-A648-8C14CB0B276E','20200319','9:51 AM','Ernest Bodie MD','Obstetrics','0')

INSERT INTO [order_] ([actDiagnosisCode],[actMood],[actStatus],[actText],[actTextDisplay],[education],[educationBy],[encounterDate],[encounterID])VALUES('R94.31','ORD','ordered','Ernest Bodie MD -Obstetrics','Referrals: Obstetrics. Ernest Bodie MD',0,NULL,'20200317','2806DE9B-294F-4A7E-8214-21B4C3E3A2C2')

INSERT INTO [order_] ([documented_by],[code_system],[refer_to_prov_id],[chk_internal_referral],[cc_to_patient])VALUES('NEXTGEN Y. Admin','SNOMED','317CD755-D003-4202-AB63-D6D802ED6ED2','N','N')


INSERT INTO [order_] ([enterprise_id],[practice_id],[person_id],[created_by],
[seq_no],[create_timestamp],[modified_by],[modify_timestamp],[actClass],[actDiagnosisCode],
[actMood],[actStatus],[actText],[actTextDisplay],[education],[educationBy],[encounterDate],
[encounterID],[locationName],[ordered],[orderedBy],[orderedByKey],[orderedDate],[orderedTime],[referToPhysician],[referToSpecialty],[userID],
[documented_by],[chk_scanreport],[code_system],[code_value],[refer_to_prov_id],[chk_internal_referral],[cc_to_patient])
select top 1 enterprise_id, practice_id, person_id,0,newid()[seq_no],CURRENT_TIMESTAMP,0,CURRENT_TIMESTAMP,'REFR',
'R94.31','ORD','ordered','Ernest Bodie MD -Obstetrics','Referrals: Obstetrics. Ernest Bodie MD',0,NULL,convert(varchar,enc_timestamp,112),'2806DE9B-294F-4A7E-8214-21B4C3E3A2C2',
'Family Practice Location',1,'Matthew IM Abbott MD','C153AB5E-9BE3-47A2-A648-8C14CB0B276E',(select format (getdate(), 'yyyyMMdd')),'9:51 AM','Ernest Bodie MD7','Obstetrics','0',
'NEXTGEN Y. Admin',0,'SNOMED','308484001','317CD755-D003-4202-AB63-D6D802ED6ED2','N','N'
from patient_encounter where enc_id = (select top 1 enc_id from patient_encounter
where person_id = (Select person_id from person where last_name = 'Siree' and first_name = 'Doc Test') order by create_timestamp DESC)

INSERT INTO [dbo].[order_management_data_]
           (enterprise_id,practice_id,person_id,seq_no,created_by,create_timestamp,modified_by,modify_timestamp,txt_act_text_display,txt_action,txt_actionDate,
		   txt_actor,txt_actstatus,txt_date,txt_order_enc_id,txt_order_id,txt_orderedby,txt_time,txt_refer_to_provider_id,txt_refer_to_physician,txt_refer_to_specialty)     
select top 1 enterprise_id, practice_id, person_id,newid()[seq_no],0,CURRENT_TIMESTAMP,0,CURRENT_TIMESTAMP,actTextDisplay,'order submitted',encounterDate,documented_by,actStatus,orderedDate,
encounterID,seq_no,orderedBy,orderedTime,refer_to_prov_id,referToPhysician,referToSpecialty 
from order_ where seq_no = (select top 1 seq_no from order_
where person_id = (Select person_id from person where last_name = 'Siree' and first_name = 'DS Test') order by create_timestamp DESC)


select 

select top 1 convert(varchar,enc_timestamp,112) from patient_encounter
where person_id = (Select person_id from person where last_name = 'Siree' and first_name = 'Doc Test')


"INSERT INTO order_ (enterprise_id, practice_id, person_id, seq_no, create_timestamp, modify_timestamp, created_by, modified_by, create_timestamp_tz, modify_timestamp_tz, 
actClass, actCompletedTime, actDiagnosis, actDiagnosisCode, actMood, actStatus, actText, actTextDisplay, education,educationBy,
encounterDate, encounterID, locationName, ordered, orderedBy, orderedByKey, orderedDate, orderedTime, referToPhysician, referToSpecialty, userID,
documented_by, chk_scanreport, code_system, code_value, refer_to_prov_id, chk_internal_referral, cc_to_patient)" &_
									" select top 1 enterprise_id, practice_id, person_id, newid()[seq_no], CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, created_by, modified_by,
									'704', '704', 'REFR', '2:31 AM', 'TC_Diagnosis_Description',  'TC_DiagnosisCode', 'ORD', 'ordered',
									'Ernest Bodie MD -Obstetrics','Referrals: Obstetrics. Ernest Bodie MD7',  1, 'NEXTGEN Admin',  convert(varchar,enc_timestamp,112), enc_id,  'TC_Location_Name',  1,
									'TC_Provider_Name',  '9644B192-8290-4A73-B7BA-A7F3FA7E222B', (select format (getdate(), 'yyyyMMdd')), '2:31 AM', 'Ernest Bodie MD7','Obstetrics','0',
									'NEXTGEN Admin', 0, 'SNOMED','308484001','317CD755-D003-4202-AB63-D6D802ED6ED2','N','N' 
									from patient_encounter where enc_id = 'TC_ENCOUNTER_ID'"


select format (getdate(), 'yyyymmdd') 


INSERT INTO order_ (enterprise_id, practice_id, person_id, seq_no, create_timestamp, modify_timestamp, created_by, modified_by,
create_timestamp_tz, modify_timestamp_tz, actClass, actCompletedTime, actDescription , actDiagnosis, actDiagnosisCode
actMood, actStatus, actSubClass, actText, actTextDispDoc, actTextDisplay, completed, completedBy, completedDate,
completedTime, encounterID, locationName, ordered, orderedBy, orderedByKey, orderedDate, orderedTime, sortOrderDisplay,
supplyQuantity, userID, documented_by, txt_dept, chk_scanreport, system_order_set)
select top 1 enterprise_id, practice_id, person_id, newid()[seq_no], CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, created_by, modified_by,
'704', '704', 'INSTRUCT', '2:31 AM', 'TestInstruction', 'TC_Diagnosis_Description',  'TC_DiagnosisCode', 'ORD', 'completed', 'GEN',
'TestInstruction', 'TestInstruction', 'TestInstruction',  1, 'NEXTGEN Admin',  getdate(), '2:31 AM', enc_id,  'TC_Location_Name',  1,
'TC_Provider_Name',  '9644B192-8290-4A73-B7BA-A7F3FA7E222B', getdate(), '2:31 AM', 1, 1.00, 0, 'NEXTGEN Admin', 'FP', 0, 'N'
from patient_encounter where enc_id = (select top 1 enc_id from patient_encounter
where person_id = (Select person_id from person where last_name = 'Siree' and first_name = 'Doc Test') order by create_timestamp DESC)