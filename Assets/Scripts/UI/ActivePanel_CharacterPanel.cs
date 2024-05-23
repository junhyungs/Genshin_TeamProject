using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivePanel_CharacterPanel : MonoBehaviour, IActivePanel
{
    IActivePanel previousPanel;
    Animator animator;
    public GameObject render_Manager;
    RenderManager renderManager;
    characterItemSettingButton characterItemSettingButton;

    bool _hasAnimator = false;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator != null) _hasAnimator = true;
        renderManager = render_Manager.GetComponent<RenderManager>();
        characterItemSettingButton = transform.GetChild(3).GetComponent<characterItemSettingButton>();
    }
    public void PanelActive(IActivePanel previousPanel)
    {
        this.previousPanel = previousPanel;
        previousPanel.DisablePanel();
        gameObject.SetActive(true);
        UIManager.Instance.activePanel = this;
        renderManager.ChangeCharacter(0);
        characterItemSettingButton.SelectActive(0);
    }

    public void PanelInactive()
    {
        UIManager.Instance.activePanel = previousPanel;
        gameObject.SetActive(false);
        if(previousPanel != null) { previousPanel.EnablePanel(); }
        
    }
    public virtual void DisablePanel()
    {
        if (_hasAnimator) animator.Play("Disable_Panel");
    }
    public virtual void EnablePanel()
    {
        if(_hasAnimator) animator.Play("Enable_Panel");
    }

}