public static class NarrativeData
{
    internal static string FormatDialogueText(string message)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            message ?? string.Empty, @"(?<!\.)\.(?!\.)(?:[ \t]+(?=\S)|(?<=[가-힣]\.)(?=[가-힣0-9]))", ".\n");
    }
    public const string EmergencyMessageSound = "SND-028_EmergencyAlert_Oneshot";
    public const string PoliceMessageSound = "SND-050_PhoneVibration_OneShot_Interval";
    // v07 개발 전달 대본의 표시 문구. 화면 선택지와 실제 발화는 별도 상수로 관리합니다.
    public const string Day1_TVNews_1 = "인천 산악지역에서 신원 미상의 시신이 잇따라 발견됐습니다.";
    public const string Day1_TVNews_2 = "경찰은 시신의 상태에 주목하고, 연관성을 조사하고 있습니다.";
    public const string Day1_TVNews = Day1_TVNews_1 + "\n" + Day1_TVNews_2;
    public const string Day1_Monologue = "내일은 좀 일찍 끝나겠지.";
    public const string Day2_Monologue = "이게 그 사건인가…";
    public const string Day2_Warning = "인천 해원지구대\n\n최근 해원동 일대에서 신원 미상의 사체가 연이어 발견되고 있습니다.\n\n발견된 사체는 일반적인 형태와 다른 상태로 확인되고 있어 정확한 원인을 조사 중입니다.\n\n주민 여러분께서는 야간 외출 및 인근 산 출입을 자제하시고, 이상 징후 발견 시 즉시 해원지구대로 신고 바랍니다.";

    public const string Day3_Warning = "인천서부경찰서\n\n금일 해원동 일대에서 사망 사건이 발생하였습니다.\n\n동일 지역 내 추가 피해가 우려되오니\n\n인적이 드문 장소 접근을 자제하시기 바랍니다.";
    public const string Day3_Man_1 = "아, 잠시만요. 죄송합니다.";
    public const string Day3_Man_2 = "4층이시네요.";
    public const string Day3_ChoiceA = "몇 층 가세요?";
    public const string Day3_ChoiceA_Line = "몇 층 가세요? 버튼 눌러드릴까요?";
    public const string Day3_ChoiceA_Reply = "6층입니다. 제가 누르죠. 4층에서 먼저 내리세요.";
    public const string Day3_ChoiceB = "왜 계속 보세요?";
    public const string Day3_ChoiceB_Line = "아까부터 왜 저만 보세요?";
    public const string Day3_ChoiceB_Reply = "죄송합니다. 아는 사람인 줄 알았어요. 저는 6층입니다. 먼저 내리세요.";
    public const string Day3_Clue = "이 냄새… 언제부터 났지?";
    public const string Day3_FuseBox = "잠깐. 두꺼비집부터.";
    public const string Day3_Hallucination = "…없어. 분명 여기 있었는데.";

    public const string Day4_Warning = "인천서부경찰서\n\n최근 해원동 일대 사건과 관련하여\n\n용의자로 추정되는 인물에 대한 제보가 접수되고 있습니다.\n\n대상은\n\n목과 얼굴 주변을 가린 상태로,\n어두운 계열의 복장을 착용한 모습으로 목격되었습니다.\n\n유사한 인물을 발견할 경우 접근을 자제하시고\n\n즉시 해원지구대로 신고 바랍니다.";
    public const string Day4_Man_1 = "아… 여기 사세요? 가족분이 걱정하시겠네요.";
    public const string Day4_ChoiceA = "어제도 봤죠?";
    public const string Day4_ChoiceA_Line = "어제 엘리베이터에서 만났잖아요. 기억 안 나세요?";
    public const string Day4_ChoiceA_Reply = "…그랬나요. 요즘 얼굴을 잘 못 알아봅니다. 이건 계단에 떨어져 있더군요. 혹시 선생님 것입니까?";
    public const string Day4_ChoiceB = "길 좀 비켜주세요.";
    public const string Day4_ChoiceB_Line = "집에 들어가야겠어요. 길 좀 비켜주세요.";
    public const string Day4_ChoiceB_Reply = "비켜드리죠. 이건 문 앞에 둘게요. 계단에서 주웠습니다.";
    public const string Day4_OpenDoor = "문을… 열어뒀던가?";
    public const string Day4_Phone = "진동 소리. 저쪽이다.";

    public const string Call112_Opening = "112입니다. 말씀하세요.";
    public const string Call112_ChoiceA = "흉기를 들고 있어요.";
    public const string Call112_ChoiceA_Line = "인천 해원동 아파트 404호예요. 침입자가 칼을 들고 있어요. 빨리 와 주세요.";
    public const string Call112_ChoiceA_Reply = "주소 확인했습니다. 문에서 떨어져 숨어 계세요. 출동합니다.";
    public const string Call112_ChoiceB = "말하면 들켜요.";
    public const string Call112_ChoiceB_Line = "인천 해원동 아파트 404호… 크게 말 못 해요. 그 사람이 바로 밖에 있어요.";
    public const string Call112_ChoiceB_Reply = "알겠습니다. 대답하지 않으셔도 됩니다. 통화만 유지하세요. 출동합니다.";

    public const string Day5_Police_1 = "인천서부경찰서 박 경위입니다. 어젯밤 신고 건으로 연락드립니다.";
    public const string Day5_Police_2 = "주거침입 혐의는 유지됩니다. 다만 살해 혐의는 증거가 부족해서, 두 시간 전에 풀려났습니다.";
    public const string Day5_Police = Day5_Police_1 + "\n" + Day5_Police_2;
    public const string Day5_ChoiceA = "다시 오면 어떡하죠?";
    public const string Day5_ChoiceA_Line = "그 사람이 다시 이 집에 오면 어떡하죠?";
    public const string Day5_ChoiceA_Reply = "문을 열지 마세요. 출동 기록은 남아 있습니다. 위협을 느끼면 바로 신고하세요.";
    public const string Day5_ChoiceB = "현장 지문은요?";
    public const string Day5_ChoiceB_Line = "그 방에서 나온 지문은요? 그 사람 것 아니에요?";
    public const string Day5_ChoiceB_Reply = "그 사람 것과 일치하는 지문은 없었습니다. 현장 검식은 계속하고 있습니다.";
    public const string Day5_TVNews_1 = "어젯밤 해원동 아파트에서 붙잡힌 연쇄 살인 사건 용의자가 석방됐습니다.";
    public const string Day5_TVNews_2 = "경찰은 현장 증거와 지문, 알리바이가 용의자와 일치하지 않는다고 밝혔습니다.";
    public const string Day5_Gem = "…이 자리에 있었어?";
}
