using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveRecord
{
    public string id; // 저장 ID SaveID.Guid
    public string type;   // 데이터 소유 타입(복원 라우팅 힌트)
    public string payload;// 실제 데이터 (JSON 문자열)
}

[Serializable]
public class SaveFile
{
    public int version = 1; // SaveFile 포맷버전 (마이그레이션 대비)
    public String sceneName; // 저장된 씬 이름
    public String savedAtUtc; // 저장된 UTC 시간 ISO8601
    public List<SaveRecord> records = new List<SaveRecord>(); // 저장된 데이터 레코드들
}

public interface ISaveable
{
    //고유 식별자 (SaveId에서 가져옴)
    string GetSaveId();
    // 현재 상태를 캡처하여 저장할 수 있는 객체 반환
    object CaptureState(); 

    //로드시 호출: 저장된 DRO를 받아 복원
    void RestoreState(object state);    
}